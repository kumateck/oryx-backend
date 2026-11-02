using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal sealed record ProductionMaterialRepairPlan(ProductionMaterialSource Source,
    ProductionMaterialConfiguration Configuration, Requisition Requisition,
    List<MaterialBatchReservedQuantity> Reservations, ProductionMaterialAllocationPlan Allocation,
    ProductionMaterialRepairPreview Preview);

internal static class ProductionMaterialRepairPlanning
{
    internal static async Task<Result<ProductionMaterialRepairPlan>> Build(ApplicationDbContext context, Guid activityId, MaterialKind kind)
    {
        var activity = await context.ProductionActivities.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == activityId);
        if (activity is null) return Error.NotFound("Repair.Activity", "The production run was not found.");
        var loaded = await ProductionMaterialSources.Load(context, activity.ProductionScheduleProductId, kind);
        if (loaded.IsFailure) return loaded.Error;
        var source = loaded.Value;
        var blockers = source.Blockers.ToList();
        if (activity.DeletedAt.HasValue || activity.Status is ProductionStatus.Completed or ProductionStatus.Cancelled)
            blockers.Add("Completed, cancelled or deleted production cannot be repaired.");
        var snapshot = await context.ProductionMaterialConfigurations.SingleOrDefaultAsync(item => item.ProductionScheduleProductId == source.Batch.Id && item.Kind == kind);
        if (snapshot is not null)
        {
            var units = await context.UnitOfMeasures.IgnoreQueryFilters().AsNoTracking().ToDictionaryAsync(item => item.Id);
            if (ProductionMaterialSources.Requirements(snapshot.RequirementsJson).Any(item =>
                !string.Equals(units.GetValueOrDefault(item.UnitId)?.Symbol?.Trim(), item.Unit.Trim(), StringComparison.OrdinalIgnoreCase)
                || !string.Equals(units.GetValueOrDefault(item.BaseUnitId)?.Symbol?.Trim(), item.BaseUnit.Trim(), StringComparison.OrdinalIgnoreCase)))
                blockers.Add("An applied unit's definition changed. Reconcile the stock units before repairing this batch.");
        }
        var reservations = await context.MaterialBatchReservedQuantities.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Include(item => item.MaterialBatch).ThenInclude(item => item.Material).Include(item => item.UoM)
            .Where(item => item.ProductionScheduleProductId == source.Batch.Id && item.MaterialBatch.Material.Kind == kind).ToListAsync();
        if (reservations.Any(item => item.DeletedAt.HasValue && item.ReleasedAt == null))
            blockers.Add("This material has consumed or returned reservation history. It cannot be repaired.");
        var active = reservations.Where(item => item.DeletedAt == null && item.ReleasedAt == null).ToList();
        if (active.Any(item => item.WarehouseId != source.ProductionId || !item.WarehouseLocationShelfId.HasValue || item.Quantity <= 0)
            || active.GroupBy(item => (item.MaterialBatchId, item.WarehouseLocationShelfId, item.UoMId)).Any(group => group.Count() != 1))
            blockers.Add("Existing allocations have ambiguous warehouse, shelf, unit or quantity evidence. Reconcile them first.");
        var prefix = kind == MaterialKind.Raw ? "RM" : "PM";
        var requisitions = await context.Requisitions.IgnoreQueryFilters().IgnoreAutoIncludes().AsSplitQuery()
            .Include(item => item.Items).ThenInclude(item => item.Material).Include(item => item.Items).ThenInclude(item => item.UoM)
            .Include(item => item.Approvals)
            .Where(item => item.ProductionScheduleProductId == source.Batch.Id && item.RequisitionType == RequisitionType.Stock
                && (item.Code.StartsWith(prefix) || item.Items.Any(line => line.Material.Kind == kind))).ToListAsync();
        var reqIds = requisitions.Select(item => item.Id).ToList();
        var issued = await context.StockRequisitionIssues.IgnoreQueryFilters().AnyAsync(item => reqIds.Contains(item.RequisitionId));
        var ledger = await context.BinCardInformation.IgnoreQueryFilters().AnyAsync(item => item.RequisitionId.HasValue
            && reqIds.Contains(item.RequisitionId.Value) && item.QuantityIssued > 0);
        if (issued || ledger || requisitions.Any(item => item.Status == RequestStatus.Completed
            || item.Items.Any(line => line.Material.Kind == kind && (line.Status == RequestStatus.Completed || line.QuantityReceived > 0))))
            blockers.Add("This material has already been issued. Issued or consumed stock cannot be repaired.");
        if (requisitions.Any(item => item.DeletedAt.HasValue || item.Items.Any(line => line.DeletedAt.HasValue && !line.RetiredByRepairId.HasValue || line.Material.Kind != kind)))
            blockers.Add("Deleted or mixed-kind stock requisition history must be reconciled before repair.");
        if (requisitions.Count > 1) blockers.Add("More than one main stock requisition exists for this material kind.");
        var requisition = requisitions.Count == 1 ? requisitions[0] : null;
        if (await context.ProductionExtraPackings.IgnoreQueryFilters().AnyAsync(item => item.ProductionScheduleProductId == source.Batch.Id && item.Material.Kind == kind))
            blockers.Add("Extra material exists for this kind. Reconcile it before repair.");
        if (await context.StockTransfers.IgnoreQueryFilters().AnyAsync(item => (item.ProductionScheduleProductId == source.Batch.Id
                || item.ProductionActivityStep.ProductionActivity.ProductionScheduleProductId == source.Batch.Id) && item.Material.Kind == kind
                && (item.Sources.Count == 0 || item.Sources.Any(part => part.Status != StockTransferStatus.Rejected || part.IssuedAt.HasValue)))
            || await context.SwapRequests.IgnoreQueryFilters().AnyAsync(item => item.StockRequisitionId.HasValue && reqIds.Contains(item.StockRequisitionId.Value) && item.Status != SwapRequestStatus.Rejected))
            blockers.Add("Stock transfer or swap evidence affects these allocations. Reconcile it before repair.");
        if (await context.Requisitions.IgnoreQueryFilters().AnyAsync(item => item.ProductionScheduleProductId == source.Batch.Id
            && item.RequisitionType == RequisitionType.Purchase && item.Status != RequestStatus.Completed && item.Status != RequestStatus.Rejected
            && item.Items.Any(line => line.Material.Kind == kind)))
            blockers.Add("An open purchase requisition affects these materials. Reconcile its procurement before repair.");
        if (await context.MaterialReturnNotes.IgnoreQueryFilters().AnyAsync(item => item.ProductionScheduleProductId == source.Batch.Id)
            || await context.FinalPackings.IgnoreQueryFilters().AnyAsync(item => item.ProductionScheduleProductId == source.Batch.Id)
            || await context.FinishedGoodsTransferNotes.IgnoreQueryFilters().AnyAsync(item => item.BatchManufacturingRecord.ProductionScheduleProductId == source.Batch.Id))
            blockers.Add("Return, final packing or finished-goods transfer history prevents this repair.");
        var allocation = await ProductionMaterialAllocator.Plan(context, source, active);
        blockers.AddRange(allocation.Blockers);
        var before = snapshot is null ? LegacyBaseline(requisition, active, blockers) : ProductionMaterialSources.Requirements(snapshot.RequirementsJson);
        var lines = Lines(before, source.Requirements, active, allocation, blockers);
        if (lines.Any(item => item.Shortage > 0)) blockers.Add("There is not enough unreserved, usable stock. No changes can be applied until all shortages are covered.");
        var desired = allocation.Allocations.OrderBy(item => item.BatchId).ThenBy(item => item.ShelfId).ToList();
        var existing = active.Select(item => new ProductionMaterialAllocation(item.MaterialBatchId, item.WarehouseLocationShelfId.GetValueOrDefault(), item.UoMId, item.Quantity))
            .OrderBy(item => item.BatchId).ThenBy(item => item.ShelfId).ToList();
        var documentsMatch = requisition is null || Matches(requisition, source.Requirements);
        if (!documentsMatch && !await context.ApprovalStages.AnyAsync(stage => stage.Required
                && stage.Approval.ItemType == "StockRequisition" && stage.Approval.DeletedAt == null))
            blockers.Add("Configure a stock-requisition approval workflow with a required stage before changing a requisition.");
        var changed = snapshot is null || snapshot.SourceHash != source.SourceHash || ProductionMaterialSources.Json(before) != ProductionMaterialSources.Json(source.Requirements)
            || ProductionMaterialSources.Json(desired) != ProductionMaterialSources.Json(existing) || !documentsMatch;
        var hash = ProductionMaterialSources.Hash(new
        {
            source.SourceHash, activity.Id, activity.Status, activity.DeletedAt, Configuration = snapshot?.RequirementsJson, snapshot?.Revision,
            Reservations = reservations.OrderBy(item => item.Id).Select(item => new { item.Id, item.MaterialBatchId, item.WarehouseId,
                item.WarehouseLocationShelfId, item.Quantity, item.UoMId, item.DeletedAt, item.ReleasedAt }),
            Documents = requisitions.OrderBy(item => item.Id).Select(item => new { item.Id, item.Status, item.Approved, item.ContentRevision,
                Items = item.Items.OrderBy(line => line.Id).Select(line => new { line.Id, line.MaterialId, line.UoMId, line.Quantity, line.QuantityReceived, line.Status, line.DeletedAt }),
                Approvals = item.Approvals.OrderBy(stage => stage.Id).Select(stage => new { stage.Id, stage.Status, stage.ApprovedById, stage.ContentRevision }) }),
            allocation.EvidenceHash, Lines = lines,
            Allocations = allocation.Allocations.OrderBy(item => item.BatchId).ThenBy(item => item.ShelfId).ThenBy(item => item.UnitId),
            Blockers = blockers.Order().ToList()
        });
        return new ProductionMaterialRepairPlan(source, snapshot, requisition, active, allocation, new ProductionMaterialRepairPreview
        {
            ActivityId = activityId, BatchId = source.Batch.Id, Kind = kind, PreviewHash = hash, SourceHash = source.SourceHash,
            Baseline = snapshot is not null ? "Applied batch configuration" : requisition is not null ? "Existing requisition; original product configuration was not recorded" : "Existing reservations; original product configuration was not recorded",
            HasChanges = changed, ApprovalRequired = requisition is not null && !documentsMatch, RequisitionId = requisition?.Id,
            Lines = lines, Blockers = blockers.Distinct().ToList()
        });
    }

    internal static bool Matches(Requisition requisition, List<ProductionMaterialRequirement> requirements) =>
        requisition.Items.Count(item => item.DeletedAt == null) == requirements.Count && requirements.All(requirement => requisition.Items.Count(line => line.MaterialId == requirement.MaterialId
            && line.UoMId == requirement.UnitId && line.Quantity == requirement.Quantity && line.DeletedAt == null) == 1);

    private static List<ProductionMaterialRequirement> LegacyBaseline(Requisition requisition, List<MaterialBatchReservedQuantity> reservations, List<string> blockers)
    {
        if (requisition is not null)
        {
            if (requisition.Items.Any(item => item.UoM is null)) blockers.Add("The existing requisition has missing units.");
            return requisition.Items.Where(item => item.DeletedAt == null).OrderBy(item => item.MaterialId).Select(item => new ProductionMaterialRequirement
            { MaterialId = item.MaterialId, Name = item.Material.Name, UnitId = item.UoMId.GetValueOrDefault(), Unit = item.UoM?.Symbol ?? "", Quantity = item.Quantity }).ToList();
        }
        return reservations.GroupBy(item => item.MaterialBatch.MaterialId).OrderBy(group => group.Key).Select(group =>
        {
            var first = group.First();
            var quantities = group.Select(item => UomConverter.Convert(item.Quantity, item.UoM?.Symbol, first.UoM?.Symbol)).ToList();
            if (quantities.Any(value => !value.HasValue)) blockers.Add("The existing reservation units cannot be reconciled.");
            return new ProductionMaterialRequirement { MaterialId = group.Key, Name = first.MaterialBatch.Material.Name,
                UnitId = first.UoMId, Unit = first.UoM?.Symbol ?? "", Quantity = quantities.Sum(value => value.GetValueOrDefault()) };
        }).ToList();
    }

    private static List<ProductionMaterialRepairLine> Lines(List<ProductionMaterialRequirement> before,
        List<ProductionMaterialRequirement> after, List<MaterialBatchReservedQuantity> reservations,
        ProductionMaterialAllocationPlan allocation, List<string> blockers) => before.Select(item => item.MaterialId)
        .Union(after.Select(item => item.MaterialId)).Order().Select(id =>
        {
            var old = before.FirstOrDefault(item => item.MaterialId == id);
            var current = after.FirstOrDefault(item => item.MaterialId == id) ?? old;
            var oldQuantity = old is null ? 0 : UomConverter.Convert(old.Quantity, old.Unit, current.Unit);
            var quantities = reservations.Where(item => item.MaterialBatch.MaterialId == id)
                .Select(item => UomConverter.Convert(item.Quantity, item.UoM?.Symbol, current.Unit)).ToList();
            if (oldQuantity is null || quantities.Any(value => !value.HasValue)) blockers.Add($"{current.Name}: the before/after units cannot be reconciled.");
            var quantity = after.FirstOrDefault(item => item.MaterialId == id)?.Quantity ?? 0;
            var shortage = allocation.Shortages.GetValueOrDefault(id);
            return new ProductionMaterialRepairLine { MaterialId = id, Name = current.Name, Unit = current.Unit, Before = oldQuantity.GetValueOrDefault(),
                After = quantity, ReservedBefore = quantities.Sum(value => value.GetValueOrDefault()), ReservedAfter = quantity - shortage, Shortage = shortage };
        }).ToList();
}
