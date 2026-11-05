using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>Swapping a repaired allocation must not retarget issued stock or only part of a held row.</summary>
internal static class ProductionMaterialSwapRules
{
    internal static async Task<Result> Validate(ApplicationDbContext context, CreateSwapRequest request)
    {
        if (!request.StockRequisitionId.HasValue) return Result.Success();
        if (request.FirstWarehouseId == request.SecondWarehouseId)
            return Error.Validation("Swap.Warehouses", "Choose two different warehouses.");
        var requisition = await context.Requisitions.IgnoreAutoIncludes().AsNoTracking().Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == request.StockRequisitionId);
        if (requisition?.RequisitionType != RequisitionType.Stock)
            return Error.Validation("Swap.Requisition", "The stock requisition was not found.");
        if (requisition.Status == RequestStatus.Completed || requisition.Items.Any(item => item.Status == RequestStatus.Completed || item.QuantityReceived > 0)
            || await context.StockRequisitionIssues.IgnoreQueryFilters().AnyAsync(item => item.RequisitionId == requisition.Id)
            || await context.BinCardInformation.IgnoreQueryFilters().AnyAsync(item => item.RequisitionId == requisition.Id && item.QuantityIssued > 0)
            || await context.MaterialBatchReservedQuantities.IgnoreQueryFilters().AnyAsync(item => item.ProductionScheduleProductId == requisition.ProductionScheduleProductId
                && item.DeletedAt != null && item.ReleasedAt == null && requisition.Items.Select(line => line.MaterialId).Contains(item.MaterialBatch.MaterialId)))
            return Error.Conflict("Swap.Issued", "Issued or consumed material cannot be retargeted by a stock swap.");
        // Only a material kind with an applied configuration has allocations to protect (packaging may be configured later).
        var requisitionMaterials = requisition.Items.Select(line => line.MaterialId).ToList();
        if (!requisition.ProductionScheduleProductId.HasValue || !await context.ProductionMaterialConfigurations.AnyAsync(item =>
                item.ProductionScheduleProductId == requisition.ProductionScheduleProductId
                && context.Materials.Any(material => requisitionMaterials.Contains(material.Id) && material.Kind == item.Kind))) return Result.Success();
        var batchId = requisition.ProductionScheduleProductId.Value;
        var first = request.FirstSwapShelfMaterialBatches;
        var second = request.SecondSwapShelfMaterialBatches;
        if (first.Count == 0 || second.Count == 0 || first.GroupBy(item => item.ShelfMaterialBatchId).Any(group => group.Count() != 1)
            || second.GroupBy(item => item.ShelfMaterialBatchId).Any(group => group.Count() != 1))
            return Error.Validation("Swap.Selection", "Select each complete source allocation and replacement shelf once.");
        var ids = first.Concat(second).Select(item => item.ShelfMaterialBatchId).ToList();
        var shelves = await context.ShelfMaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Include(item => item.MaterialBatch).ThenInclude(item => item.Material)
            .Include(item => item.WarehouseLocationShelf).ThenInclude(item => item.WarehouseLocationRack)
                .ThenInclude(item => item.WarehouseLocation)
            .Where(item => ids.Contains(item.Id) && item.DeletedAt == null && item.MaterialBatch.DeletedAt == null).ToDictionaryAsync(item => item.Id);
        var reservations = await context.MaterialBatchReservedQuantities.IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.ProductionScheduleProductId == batchId).ToListAsync();
        foreach (var item in first)
        {
            if (!shelves.TryGetValue(item.ShelfMaterialBatchId, out var shelf)
                || shelf.MaterialBatchId != item.MaterialBatchId || shelf.UoMId != item.UoMId || item.Quantity <= 0 || shelf.Quantity < item.Quantity
                || shelf.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId != request.FirstWarehouseId
                || !requisition.Items.Any(line => line.MaterialId == shelf.MaterialBatch.MaterialId && line.UoMId == item.UoMId)
                || reservations.Count(row => row.MaterialBatchId == item.MaterialBatchId && row.WarehouseLocationShelfId == shelf.WarehouseLocationShelfId
                    && row.UoMId == item.UoMId && row.Quantity == item.Quantity) != 1)
                return Error.Conflict("Swap.StaleAllocation", "The allocation changed. Reload and select a complete current allocation before swapping.");
        }
        var totals = first.GroupBy(item => (shelves[item.ShelfMaterialBatchId].MaterialBatch.MaterialId, item.UoMId))
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        var replacementIds = second.Select(item => item.MaterialBatchId).ToList();
        var otherHolds = await ProductionMaterialAllocator.UnissuedReservations(context).IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.ProductionScheduleProductId != batchId && replacementIds.Contains(item.MaterialBatchId)).ToListAsync();
        foreach (var item in second)
        {
            if (!shelves.TryGetValue(item.ShelfMaterialBatchId, out var shelf)
                || shelf.MaterialBatchId != item.MaterialBatchId || shelf.UoMId != item.UoMId || item.Quantity <= 0
                || shelf.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId != request.SecondWarehouseId
                || shelf.MaterialBatch.Status != BatchStatus.Available
                || MaterialValidity.IsExpired(shelf.MaterialBatch, DateTime.UtcNow))
                return Error.Conflict("Swap.Replacement", "The replacement shelf, units or stock eligibility changed. Reload before swapping.");
            var held = otherHolds.Where(row => row.MaterialBatchId == item.MaterialBatchId && row.WarehouseLocationShelfId == shelf.WarehouseLocationShelfId).ToList();
            var key = (shelf.MaterialBatch.MaterialId, item.UoMId);
            if (held.Any(row => row.UoMId != item.UoMId) || shelf.Quantity - held.Sum(row => row.Quantity) < item.Quantity
                || !totals.TryGetValue(key, out var remaining) || remaining < item.Quantity)
                return Error.Conflict("Swap.Reserved", "The replacement is reserved for another batch or does not match the selected material and unit.");
            totals[key] -= item.Quantity;
        }
        return totals.Values.Any(value => value != 0)
            ? Error.Validation("Swap.QuantityMismatch", "Replacement quantities must match the complete allocations by material and unit.") : Result.Success();
    }
}
