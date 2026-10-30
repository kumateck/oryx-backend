using APP.Services.Authorization;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules.Packing;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Routes;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class ProductionStepRecordsQuery
{
    internal static async Task<Result<ProductionStepRecordsDto>> Read(
        ApplicationDbContext context, IMapper mapper, Guid activityId, Guid stepId,
        Guid actorId, CancellationToken cancellationToken)
    {
        if (!(await CurrentPermissionSet.Load(context, actorId)).Contains(PermissionKeys.CanViewPlannedProducts))
            return Error.Forbidden("ProductionStep.Permission", "Production board access is required.");
        var actor = await context.Users.IgnoreAutoIncludes().AsNoTracking()
            .Where(user => user.Id == actorId && !user.IsDisabled && user.DeletedAt == null)
            .Select(user => new { user.DepartmentId, Type = (DepartmentType?)user.Department.Type })
            .SingleOrDefaultAsync(cancellationToken);
        if (actor?.Type != DepartmentType.Production || !actor.DepartmentId.HasValue)
            return Error.Forbidden("ProductionStep.Department", "An assigned production department is required.");
        var step = await context.ProductionActivitySteps.IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.Id == stepId && item.ProductionActivityId == activityId
                && item.DeletedAt == null && item.ProductionActivity.DeletedAt == null
                && item.ProductionActivity.ProductionScheduleProduct.Product.DeletedAt == null
                && item.ProductionActivity.ProductionScheduleProduct.Product.DepartmentId == actor.DepartmentId
                && item.ProductionActivity.ProductionScheduleProduct.ProductionSchedule.DeletedAt == null)
            .Select(item => new
            {
                BatchId = item.ProductionActivity.ProductionScheduleProductId,
                Actions = item.ResponsibleUsers.Where(user => user.DeletedAt == null)
                    .Select(user => user.Action).ToList()
            }).SingleOrDefaultAsync(cancellationToken);
        if (step is null) return Error.NotFound("ProductionStep.NotFound", "The step was not found in this production run and department.");

        var result = new ProductionStepRecordsDto { StepId = stepId, ProductionScheduleProductId = step.BatchId };
        // Exact step-linked evidence is always read, even when its instruction was
        // changed later. Batch-only legacy evidence is selected by the action and
        // explicitly identified as batch context, including on cancelled runs.
        await ReadAnalysis(context, mapper, result, cancellationToken);
        await ReadManufacturing(context, mapper, result, step.Actions.Contains(OperationAction.BmrAndBprRequisitionOrIssue), cancellationToken);
        await ReadRequisitions(context, mapper, result, step.Actions.Contains(OperationAction.StockRequisitionOrIssue), cancellationToken);
        await ReadPacking(context, mapper, result, step.Actions.Contains(OperationAction.FinalPackingAndPartialReturn), cancellationToken);
        await ReadTransfers(context, mapper, result, step.Actions.Contains(OperationAction.TransferFinishedProducts), cancellationToken);
        if (step.Actions.Contains(OperationAction.AdditionalStockRequestOrIssue))
            await ReadExtraMaterial(context, result, cancellationToken);
        if (step.Actions.Contains(OperationAction.FullReturn) || step.Actions.Contains(OperationAction.FinalPackingAndPartialReturn))
            await ReadReturns(context, mapper, result, step.Actions.Contains(OperationAction.FullReturn), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    private static async Task ReadAnalysis(ApplicationDbContext context, IMapper mapper,
        ProductionStepRecordsDto result, CancellationToken token)
    {
        var requests = await context.AnalyticalTestRequests.IgnoreAutoIncludes().AsNoTracking().AsSplitQuery()
            .Include(item => item.CreatedBy).Include(item => item.SampledBy).Include(item => item.AcknowledgedBy)
            .Include(item => item.ReleasedBy).Include(item => item.IssuedBy).Include(item => item.TestedBy)
            .Include(item => item.Assignees).ThenInclude(item => item.User)
            .Include(item => item.State).Include(item => item.BatchManufacturingRecord)
            .Where(item => item.ProductionActivityStepId == result.StepId
                && item.ProductionScheduleProductId == result.ProductionScheduleProductId && item.DeletedAt == null)
            .OrderBy(item => item.CreatedAt).ThenBy(item => item.Id).ToListAsync(token);
        result.AnalyticalTests = requests.Select(item =>
        {
            var request = mapper.Map<AnalyticalTestRequestDto>(item);
            request.Assignees = mapper.Map<List<DOMAIN.Entities.Users.UserDto>>(item.Assignees
                .Where(assignee => assignee.DeletedAt == null && assignee.User != null).Select(assignee => assignee.User));
            return new ProductionStepAnalysisDto
            {
                Request = request, ChemicalArdId = item.ChemicalArdId, MicrobialArdId = item.MicrobialArdId,
                MicrobialRequired = item.MicrobialRequired, TestedAt = item.TestedAt, TestedBy = Person(item.TestedBy)
            };
        }).ToList();
    }

    private static async Task ReadManufacturing(ApplicationDbContext context, IMapper mapper,
        ProductionStepRecordsDto result, bool includeBatch, CancellationToken token)
    {
        var rows = await context.BatchManufacturingRecords.IgnoreAutoIncludes().AsNoTracking()
            .Include(item => item.CreatedBy)
            .Where(item => item.ProductionScheduleProductId == result.ProductionScheduleProductId && item.DeletedAt == null
                && (item.ProductionActivityStepId == result.StepId || includeBatch))
            .OrderBy(item => item.CreatedAt).ThenBy(item => item.Id).ToListAsync(token);
        result.BatchRecords = mapper.Map<List<BatchManufacturingRecordDto>>(rows);
        result.BatchScopedRecordIds.AddRange(rows.Where(item => item.ProductionActivityStepId != result.StepId).Select(item => item.Id));
    }

    private static async Task ReadRequisitions(ApplicationDbContext context, IMapper mapper,
        ProductionStepRecordsDto result, bool includeBatch, CancellationToken token)
    {
        var rows = await context.Requisitions.IgnoreAutoIncludes().AsNoTracking().AsSplitQuery()
            .Include(item => item.RequestedBy).Include(item => item.Department)
            .Include(item => item.Items).ThenInclude(item => item.Material)
            .Include(item => item.Items).ThenInclude(item => item.UoM)
            .Where(item => item.ProductionScheduleProductId == result.ProductionScheduleProductId && item.DeletedAt == null
                && (item.ProductionActivityStepId == result.StepId || includeBatch && item.ProductionActivityStepId == null))
            .OrderBy(item => item.CreatedAt).ThenBy(item => item.Id).ToListAsync(token);
        foreach (var row in rows) row.Items = row.Items.Where(item => item.DeletedAt == null).ToList();
        result.Requisitions = mapper.Map<List<RequisitionDto>>(rows);
        result.BatchScopedRecordIds.AddRange(rows.Where(item => item.ProductionActivityStepId == null).Select(item => item.Id));
    }

    private static async Task ReadPacking(ApplicationDbContext context, IMapper mapper,
        ProductionStepRecordsDto result, bool includeBatch, CancellationToken token)
    {
        var rows = await context.FinalPackings.IgnoreAutoIncludes().AsNoTracking().AsSplitQuery()
            .Include(item => item.CreatedBy).Include(item => item.ProductPacking)
            .Include(item => item.Materials).ThenInclude(item => item.Material)
            .Where(item => item.ProductionScheduleProductId == result.ProductionScheduleProductId && item.DeletedAt == null
                && (item.ProductionActivityStepId == result.StepId || includeBatch && item.ProductionActivityStepId == null))
            .OrderBy(item => item.CreatedAt).ThenBy(item => item.Id).ToListAsync(token);
        foreach (var row in rows) row.Materials = row.Materials.Where(item => item.DeletedAt == null).ToList();
        result.FinalPackings = mapper.Map<List<FinalPackingDto>>(rows);
        result.BatchScopedRecordIds.AddRange(rows.Where(item => item.ProductionActivityStepId == null).Select(item => item.Id));
    }

    private static async Task ReadTransfers(ApplicationDbContext context, IMapper mapper,
        ProductionStepRecordsDto result, bool includeBatch, CancellationToken token)
    {
        result.Transfers = await context.FinishedGoodsTransferNotes.IgnoreAutoIncludes().AsNoTracking()
            .Where(item => (item.ProductionActivityStepId == result.StepId || includeBatch && item.ProductionActivityStepId == null) && item.DeletedAt == null
                && item.BatchManufacturingRecord.ProductionScheduleProductId == result.ProductionScheduleProductId)
            .OrderBy(item => item.CreatedAt).ThenBy(item => item.Id)
            .Select(item => new FinishedGoodsTransferNoteDto
            {
                Id = item.Id, CreatedAt = item.CreatedAt,
                CreatedBy = item.CreatedBy == null ? null : new UserDto
                    { Id = item.CreatedBy.Id, FirstName = item.CreatedBy.FirstName, LastName = item.CreatedBy.LastName },
                TransferNoteNumber = item.TransferNoteNumber,
                ProductionActivityStep = item.ProductionActivityStepId.HasValue
                    ? new ProductionActivityStepDto { Id = item.ProductionActivityStepId.Value } : null,
                FromWarehouse = item.FromWarehouse == null ? null : new DOMAIN.Entities.Warehouses.WarehouseDto
                    { Id = item.FromWarehouse.Id, Name = item.FromWarehouse.Name },
                ToWarehouse = item.ToWarehouse == null ? null : new DOMAIN.Entities.Warehouses.WarehouseDto
                    { Id = item.ToWarehouse.Id, Name = item.ToWarehouse.Name },
                UoM = item.UoM == null ? null : new DOMAIN.Entities.Base.UnitOfMeasureDto
                    { Id = item.UoM.Id, Name = item.UoM.Name, Symbol = item.UoM.Symbol },
                ProductPacking = item.ProductPacking == null ? null : new DOMAIN.Entities.Products.ProductPackingDto
                    { Id = item.ProductPacking.Id, Name = item.ProductPacking.Name },
                TotalQuantity = item.TotalQuantity, QuantityReceived = item.QuantityReceived,
                QuantityPerPack = item.QuantityPerPack, Loose = item.Loose,
                AllocatedQuantity = item.AllocatedQuantity, RemainingQuantity = (item.IsApproved && item.QuantityReceived > 0 ? item.QuantityReceived : item.TotalQuantity) - item.AllocatedQuantity,
                Approved = item.Approved, IsApproved = item.IsApproved, IsPartial = item.IsPartial,
                IsFulfilled = item.IsFulfilled, QarNumber = item.QarNumber, Notes = item.Notes
            }).ToListAsync(token);
        result.BatchScopedRecordIds.AddRange(result.Transfers.Where(item => item.ProductionActivityStep == null).Select(item => item.Id));
    }

    private static async Task ReadExtraMaterial(ApplicationDbContext context,
        ProductionStepRecordsDto result, CancellationToken token)
    {
        result.ExtraMaterialRequests = await context.ProductionExtraPackings.IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.ProductionScheduleProductId == result.ProductionScheduleProductId && item.DeletedAt == null)
            .OrderBy(item => item.CreatedAt).ThenBy(item => item.Id)
            .Select(item => new ProductionStepExtraMaterialDto
            {
                Id = item.Id, Material = new CollectionItemDto { Id = item.MaterialId, Name = item.Material.Name, Code = item.Material.Code },
                Unit = item.UoM == null ? null : new CollectionItemDto { Id = item.UoM.Id, Name = item.UoM.Symbol },
                Quantity = item.Quantity, Status = (int)item.Status, Approved = item.Approved,
                CreatedAt = item.CreatedAt, CreatedBy = item.CreatedBy == null ? null : new CollectionItemDto
                    { Id = item.CreatedBy.Id, Name = item.CreatedBy.FirstName + " " + item.CreatedBy.LastName },
                IssuedAt = item.IssuedAt, IssuedBy = item.IssuedBy == null ? null : new CollectionItemDto
                    { Id = item.IssuedBy.Id, Name = item.IssuedBy.FirstName + " " + item.IssuedBy.LastName }
            }).ToListAsync(token);
        result.BatchScopedRecordIds.AddRange(result.ExtraMaterialRequests.Select(item => item.Id));
    }

    private static async Task ReadReturns(ApplicationDbContext context, IMapper mapper,
        ProductionStepRecordsDto result, bool includeFull, CancellationToken token)
    {
        var rows = await context.MaterialReturnNotes.IgnoreAutoIncludes().AsNoTracking().AsSplitQuery()
            .Include(item => item.CreatedBy)
            .Include(item => item.FullReturns).ThenInclude(item => item.MaterialBatchReservedQuantity).ThenInclude(item => item.MaterialBatch).ThenInclude(item => item.Material)
            .Include(item => item.FullReturns).ThenInclude(item => item.MaterialBatchReservedQuantity).ThenInclude(item => item.UoM)
            .Include(item => item.FullReturns).ThenInclude(item => item.DestinationWarehouse)
            .Include(item => item.PartialReturns).ThenInclude(item => item.Material)
            .Include(item => item.PartialReturns).ThenInclude(item => item.MaterialBatch)
            .Include(item => item.PartialReturns).ThenInclude(item => item.UoM)
            .Include(item => item.PartialReturns).ThenInclude(item => item.DestinationWarehouse)
            .Where(item => item.ProductionScheduleProductId == result.ProductionScheduleProductId && item.DeletedAt == null
                && (includeFull ? item.FullReturns.Any() : item.PartialReturns.Any()))
            .OrderBy(item => item.CreatedAt).ThenBy(item => item.Id).ToListAsync(token);
        foreach (var row in rows)
        {
            row.FullReturns = row.FullReturns.Where(item => item.DeletedAt == null).ToList();
            row.PartialReturns = row.PartialReturns.Where(item => item.DeletedAt == null).ToList();
        }
        result.MaterialReturns = mapper.Map<List<MaterialReturnNoteDto>>(rows);
        result.BatchScopedRecordIds.AddRange(rows.Select(item => item.Id));
    }

    private static CollectionItemDto Person(User user) => user == null ? null
        : new CollectionItemDto { Id = user.Id, Name = $"{user.FirstName} {user.LastName}".Trim() };
}
