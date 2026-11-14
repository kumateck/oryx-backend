using APP.Repository;
using APP.Services.Authorization;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Production;

/// <summary>
/// The store issuing a raw-material stock requisition decides which option of a BOM line (the main material or a
/// substitute, each with its own quantity) leaves the store. Only one option is ever issued for a line. Changing it moves
/// the batch's reservation to the chosen material and rewrites the requisition line, before anything is issued.
/// </summary>
public sealed class StockRequisitionMaterialChoiceService(ApplicationDbContext db, IMapper mapper)
{
    private sealed record Context(Requisition Requisition, ProductionMaterialConfiguration Configuration,
        List<ProductionMaterialRequirement> Requirements, ProductionMaterialSource Source);

    public async Task<Result<List<StockRequisitionMaterialOptionsDto>>> Options(Guid requisitionId)
    {
        var loaded = await Load(requisitionId, tracked: false);
        if (loaded.IsFailure) return loaded.Error;
        if (loaded.Value is null) return new List<StockRequisitionMaterialOptionsDto>();
        var (requisition, configuration, requirements, source) = loaded.Value;
        var locked = await LockReason(requisition);
        var ids = requirements.SelectMany(item => item.Options ?? []).Select(item => item.MaterialId)
            .Concat(requirements.Select(item => item.SourceMaterialId)).Distinct().ToList();
        var materials = await db.Materials.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(item => ids.Contains(item.Id)).ToDictionaryAsync(item => item.Id);
        var units = await db.UnitOfMeasures.IgnoreQueryFilters().AsNoTracking().ToDictionaryAsync(item => item.Id);
        var own = await OwnReservations(requisition, tracked: false);
        var result = new List<StockRequisitionMaterialOptionsDto>();
        foreach (var requirement in requirements.Where(item => item.Options is { Count: > 1 }))
        {
            var line = requisition.Items.SingleOrDefault(item => item.MaterialId == requirement.MaterialId && item.DeletedAt == null);
            if (line is null) continue;
            var options = new List<StockRequisitionMaterialOptionDto>();
            foreach (var option in requirement.Options)
            {
                var stock = await ProductionMaterialChoices.Evaluate(db, source, requirement, option,
                    own.Where(item => item.MaterialBatch.MaterialId == option.MaterialId).ToList());
                var batchIds = stock.Allocations.Select(item => item.BatchId).Distinct().ToList();
                var batches = await db.MaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                    .Where(item => batchIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id);
                options.Add(new StockRequisitionMaterialOptionDto
                {
                    Material = Item(materials.GetValueOrDefault(option.MaterialId), option.Name), IsMain = option.MaterialId == requirement.SourceMaterialId,
                    IsCurrent = option.MaterialId == requirement.MaterialId, Quantity = option.Quantity,
                    Unit = mapper.Map<UnitOfMeasureDto>(units.GetValueOrDefault(option.UnitId)), Available = stock.Available, Covers = stock.Covers,
                    EarliestExpiry = stock.EarliestExpiry,
                    Batches = stock.Allocations.GroupBy(item => item.BatchId).Select(group => new StockRequisitionMaterialOptionBatchDto
                    {
                        MaterialBatchId = group.Key, BatchNumber = batches.GetValueOrDefault(group.Key)?.BatchNumber, Quantity = group.Sum(item => item.Quantity),
                        Expiry = batches.TryGetValue(group.Key, out var batch) ? MaterialValidity.EffectiveDate(batch) : null
                    }).ToList()
                });
            }
            result.Add(new StockRequisitionMaterialOptionsDto
            {
                RequisitionItemId = line.Id, SourceMaterial = Item(materials.GetValueOrDefault(requirement.SourceMaterialId), requirement.Name),
                CurrentMaterialId = requirement.MaterialId, Revision = configuration.Revision, CanChange = locked is null, Reason = locked, Options = options
            });
        }
        return result;
    }

    public async Task<Result> Choose(Guid requisitionId, ChooseStockRequisitionMaterialRequest request, Guid actorId)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500)
            return Error.Validation("Substitute.Reason", "Give the reason for issuing this material (up to 500 characters).");
        await using var transaction = await ProductionMaterialTransaction.Begin(db);
        if (!(await CurrentPermissionSet.Load(db, actorId)).Contains(PermissionKeys.CanChooseRawMaterialRequisitionSubstitute))
            return Error.Forbidden("Substitute.Permission", "You do not have permission to choose the material issued for a requisition.");
        var loaded = await Load(requisitionId, tracked: true);
        if (loaded.IsFailure) return loaded.Error;
        if (loaded.Value is null) return Error.Validation("Substitute.NotConfigured", "This requisition's batch has no applied raw-material configuration.");
        var (requisition, configuration, requirements, source) = loaded.Value;

        var store = await db.Users.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(user => user.Id == actorId && user.DeletedAt == null && !user.IsDisabled)
            .SelectMany(user => user.Department.Warehouses.Where(warehouse => warehouse.Type == WarehouseType.RawMaterialStorage && warehouse.DeletedAt == null))
            .Select(warehouse => warehouse.Id).ToListAsync();
        if (!store.Contains(configuration.StorageWarehouseId))
            return Error.Forbidden("Substitute.Store", "This requisition is issued from another store's raw-material warehouse.");
        var locked = await LockReason(requisition);
        if (locked is not null) return Error.Conflict("Substitute.Locked", locked);
        if (configuration.Revision != request.Revision)
            return Error.Conflict("Substitute.Stale", "The batch's materials changed. Reload the requisition and choose again.");
        var requirement = requirements.SingleOrDefault(item => item.SourceMaterialId == request.SourceMaterialId);
        var option = requirement?.Options?.SingleOrDefault(item => item.MaterialId == request.MaterialId);
        if (requirement is null || option is null)
            return Error.Validation("Substitute.Option", "The BOM does not allow this material for that line.");
        if (option.MaterialId == requirement.MaterialId)
            return Error.Validation("Substitute.Unchanged", "This line is already issued as that material.");
        if (requirements.Any(item => item != requirement && item.MaterialId == option.MaterialId))
            return Error.Conflict("Substitute.Overlap", "That material is already issued for another BOM line of this batch.");
        var lines = requisition.Items.Where(item => item.MaterialId == requirement.MaterialId && item.DeletedAt == null).ToList();
        if (lines.Count != 1 || lines[0].UoMId != requirement.UnitId || lines[0].Quantity != requirement.Quantity)
            return Error.Conflict("Substitute.Requisition", "The requisition line no longer matches the batch's requirement. Reconcile the batch first.");
        var line = lines[0];

        var before = await ProductionMaterialSources.Load(db, source.Batch.Id, MaterialKind.Raw);
        var inSync = before.IsSuccess && before.Value.SourceHash == configuration.SourceHash;
        var own = await OwnReservations(requisition, tracked: true);
        var previous = own.Where(item => item.MaterialBatch.MaterialId == requirement.MaterialId).ToList();
        var existing = own.Where(item => item.MaterialBatch.MaterialId == option.MaterialId).ToList();
        var stock = await ProductionMaterialChoices.Evaluate(db, source, requirement, option, existing);
        if (!stock.Covers)
            return Error.Conflict("Substitute.Shortage", string.Join(" ", stock.Blockers.Concat(stock.ShortageDetails)
                .DefaultIfEmpty($"{option.Name} does not have enough usable, unreserved stock in this store.")));

        var now = DateTime.UtcNow;
        foreach (var row in previous) row.ReleasedAt = now;
        ProductionMaterialAllocator.Apply(db, source, existing, stock.Allocations);
        var chosen = ProductionMaterialSources.Requirement(requirement, option);
        var updated = requirements.Select(item => item == requirement ? chosen : item).ToList();
        configuration.RequirementsJson = ProductionMaterialSources.Json(updated);
        configuration.Revision++;
        db.StockRequisitionMaterialChoices.Add(new StockRequisitionMaterialChoice
        {
            RequisitionId = requisition.Id, RequisitionItemId = line.Id, ProductionScheduleProductId = source.Batch.Id,
            SourceMaterialId = requirement.SourceMaterialId, FromMaterialId = requirement.MaterialId, FromQuantity = requirement.Quantity,
            FromUnitId = requirement.UnitId, ToMaterialId = option.MaterialId, ToQuantity = option.Quantity, ToUnitId = option.UnitId,
            ChosenById = actorId, ChosenAt = now, Reason = request.Reason.Trim(), CreatedById = actorId
        });
        line.MaterialId = option.MaterialId;
        line.UoMId = option.UnitId;
        line.Quantity = option.Quantity;
        line.LastUpdatedById = actorId;
        var activityId = await db.ProductionActivities.IgnoreQueryFilters().Where(item => item.ProductionScheduleProductId == source.Batch.Id && item.DeletedAt == null)
            .Select(item => (Guid?)item.Id).FirstOrDefaultAsync();
        if (activityId.HasValue)
            db.ProductionActivityLogs.Add(new ProductionActivityLog { ProductionActivityId = activityId.Value, UserId = actorId, Timestamp = now,
                Message = $"Store chose {option.Name} ({Amount(option.Quantity, option.Unit)}) in place of {requirement.Name} ({Amount(requirement.Quantity, requirement.Unit)}) on {requisition.Code}. Reason: {request.Reason.Trim()}" });
        await db.SaveChangesAsync();

        // Keep the configuration current with the BOM when it was current before: the BOM read now selects the chosen option.
        if (inSync)
        {
            var after = await ProductionMaterialSources.Load(db, source.Batch.Id, MaterialKind.Raw);
            if (after.IsSuccess && ProductionMaterialSources.Json(after.Value.Requirements) == configuration.RequirementsJson)
            {
                configuration.SourceHash = after.Value.SourceHash;
                await db.SaveChangesAsync();
            }
        }
        await transaction.CommitAsync();
        return Result.Success();
    }

    private async Task<Result<Context>> Load(Guid requisitionId, bool tracked)
    {
        var query = db.Requisitions.IgnoreAutoIncludes().Include(item => item.Items.Where(line => line.DeletedAt == null)).AsQueryable();
        var requisition = await (tracked ? query : query.AsNoTracking()).SingleOrDefaultAsync(item => item.Id == requisitionId);
        if (requisition is null) return RequisitionErrors.NotFound(requisitionId);
        if (requisition.RequisitionType != RequisitionType.Stock || requisition.ProductionScheduleProductId is null)
            return Error.Validation("Substitute.Requisition", "Only a production batch's stock requisition can change the material it issues.");
        var configurations = db.ProductionMaterialConfigurations.Where(item => item.ProductionScheduleProductId == requisition.ProductionScheduleProductId
            && item.Kind == MaterialKind.Raw);
        var configuration = await (tracked ? configurations : configurations.AsNoTracking()).SingleOrDefaultAsync();
        if (configuration is null) return Result.Success<Context>(null);
        var batch = await db.ProductionScheduleProducts.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .SingleAsync(item => item.Id == requisition.ProductionScheduleProductId);
        var requirements = ProductionMaterialSources.Requirements(configuration.RequirementsJson);
        // Only the requisition's own raw lines can be changed here.
        var requested = requisition.Items.Select(item => item.MaterialId).ToHashSet();
        if (!requirements.Any(item => requested.Contains(item.MaterialId))) return Result.Success<Context>(null);
        var source = new ProductionMaterialSource(batch, configuration.DepartmentId, configuration.StorageWarehouseId,
            configuration.ProductionWarehouseId, requirements, configuration.SourceHash, string.Empty, []);
        return new Context(requisition, configuration, requirements, source);
    }

    private Task<List<MaterialBatchReservedQuantity>> OwnReservations(Requisition requisition, bool tracked)
    {
        var query = db.MaterialBatchReservedQuantities.IgnoreQueryFilters().IgnoreAutoIncludes().Include(item => item.MaterialBatch)
            .Where(item => item.ProductionScheduleProductId == requisition.ProductionScheduleProductId && item.DeletedAt == null && item.ReleasedAt == null);
        return (tracked ? query : query.AsNoTracking()).ToListAsync();
    }

    /// <summary>Why the material can no longer change: once anything has left the store, the issue stands.</summary>
    private async Task<string> LockReason(Requisition requisition)
    {
        if (requisition.Status == RequestStatus.Rejected) return "The requisition was rejected.";
        if (requisition.Status == RequestStatus.Completed || requisition.Items.Any(item => item.Status == RequestStatus.Completed || item.QuantityReceived > 0)
            || await db.StockRequisitionIssues.IgnoreQueryFilters().AnyAsync(item => item.RequisitionId == requisition.Id)
            || await db.BinCardInformation.IgnoreQueryFilters().AnyAsync(item => item.RequisitionId == requisition.Id && item.QuantityIssued > 0))
            return "The requisition has been issued.";
        var materials = requisition.Items.Select(item => item.MaterialId).ToList();
        if (await db.MaterialBatchReservedQuantities.IgnoreQueryFilters().AnyAsync(item => item.ProductionScheduleProductId == requisition.ProductionScheduleProductId
                && item.DeletedAt != null && item.ReleasedAt == null && materials.Contains(item.MaterialBatch.MaterialId)))
            return "Production has already consumed this material.";
        return null;
    }

    private static CollectionItemDto Item(Material material, string fallback) =>
        new() { Id = material?.Id ?? Guid.Empty, Name = material?.Name ?? fallback, Code = material?.Code };

    private static string Amount(decimal value, string unit) =>
        $"{value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)} {unit}";
}

/// <summary>Shows, on each requisition line, the BOM line's main material when the store chose to issue a substitute.</summary>
public static class StockRequisitionMaterialChoiceRead
{
    public static async Task Apply(ApplicationDbContext db, RequisitionDto requisition)
    {
        if (requisition?.Items is not { Count: > 0 }) return;
        var choices = await db.StockRequisitionMaterialChoices.IgnoreQueryFilters().AsNoTracking()
            .Where(choice => choice.RequisitionId == requisition.Id && choice.DeletedAt == null)
            .OrderByDescending(choice => choice.ChosenAt)
            .Select(choice => new { choice.RequisitionItemId, choice.ToMaterialId, choice.SourceMaterialId,
                Source = new CollectionItemDto { Id = choice.SourceMaterial.Id, Name = choice.SourceMaterial.Name, Code = choice.SourceMaterial.Code } })
            .ToListAsync();
        foreach (var item in requisition.Items)
        {
            var latest = choices.FirstOrDefault(choice => choice.RequisitionItemId == item.Id);
            if (latest is not null && latest.ToMaterialId != latest.SourceMaterialId && item.Material?.Id == latest.ToMaterialId)
                item.SubstituteFor = latest.Source;
        }
    }
}
