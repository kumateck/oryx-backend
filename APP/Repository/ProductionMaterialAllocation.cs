using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal sealed record ProductionMaterialAllocation(Guid BatchId, Guid ShelfId, Guid UnitId, decimal Quantity);
internal sealed record ProductionMaterialAllocationPlan(List<ProductionMaterialAllocation> Allocations,
    Dictionary<Guid, decimal> Shortages, string EvidenceHash, List<string> Blockers);

internal static class ProductionMaterialAllocator
{
    internal static IQueryable<MaterialBatchReservedQuantity> UnissuedReservations(ApplicationDbContext context) =>
        context.MaterialBatchReservedQuantities.IgnoreQueryFilters().Where(item => item.DeletedAt == null && item.ReleasedAt == null
            && (!context.RequisitionItems.IgnoreQueryFilters().Any(line => line.Requisition.ProductionScheduleProductId == item.ProductionScheduleProductId
                && line.Requisition.RequisitionType == DOMAIN.Entities.Requisitions.RequisitionType.Stock
                && line.MaterialId == item.MaterialBatch.MaterialId && (line.Status == DOMAIN.Entities.Requisitions.RequestStatus.Completed
                    || line.Requisition.Status == DOMAIN.Entities.Requisitions.RequestStatus.Completed
                    || context.StockRequisitionIssues.IgnoreQueryFilters().Any(issue => issue.RequisitionId == line.RequisitionId)))
                // Ambiguous legacy duplicates with an outstanding stock document still hold stock.
                || context.RequisitionItems.IgnoreQueryFilters().Any(line => line.Requisition.ProductionScheduleProductId == item.ProductionScheduleProductId
                    && line.Requisition.RequisitionType == DOMAIN.Entities.Requisitions.RequisitionType.Stock
                    && line.MaterialId == item.MaterialBatch.MaterialId && line.DeletedAt == null && line.Requisition.DeletedAt == null
                    && line.Status != DOMAIN.Entities.Requisitions.RequestStatus.Completed && line.Requisition.Status != DOMAIN.Entities.Requisitions.RequestStatus.Completed
                    && !context.StockRequisitionIssues.IgnoreQueryFilters().Any(issue => issue.RequisitionId == line.RequisitionId))));

    internal static async Task<ProductionMaterialAllocationPlan> Plan(ApplicationDbContext context, ProductionMaterialSource source,
        List<MaterialBatchReservedQuantity> existing)
    {
        var ids = source.Requirements.Select(item => item.MaterialId).ToList();
        var shelves = await context.ShelfMaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Include(item => item.MaterialBatch).ThenInclude(item => item.UoM)
            .Include(item => item.MaterialBatch).ThenInclude(item => item.Material)
            .Where(item => ids.Contains(item.MaterialBatch.MaterialId) && item.DeletedAt == null && item.MaterialBatch.DeletedAt == null
                && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId == source.StorageId
                && item.WarehouseLocationShelf.DeletedAt == null && item.WarehouseLocationShelf.WarehouseLocationRack.DeletedAt == null
                && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.DeletedAt == null)
            .ToListAsync();
        var shelfIds = shelves.Select(item => item.WarehouseLocationShelfId).ToList();
        // Issued reservations still exist, but their stock has already left this storage shelf.
        // Subtract only allocations which have not been issued; count all other batches' holds.
        var others = await UnissuedReservations(context).IgnoreAutoIncludes().AsNoTracking()
            .Include(item => item.UoM).Include(item => item.MaterialBatch)
            .Where(item => item.ProductionScheduleProductId != source.Batch.Id
                && item.WarehouseLocationShelfId.HasValue && shelfIds.Contains(item.WarehouseLocationShelfId.Value))
            .ToListAsync();
        var allocations = new List<ProductionMaterialAllocation>();
        var shortages = new Dictionary<Guid, decimal>();
        var blockers = new List<string>();
        foreach (var requirement in source.Requirements)
        {
            var remaining = requirement.Quantity;
            var candidates = shelves.Where(item => item.MaterialBatch.MaterialId == requirement.MaterialId
                    && item.Quantity > 0 && item.MaterialBatch.Status == BatchStatus.Available
                    && !MaterialValidity.IsExpired(item.MaterialBatch, DateTime.UtcNow))
                .OrderByDescending(item => existing.Any(reservation => reservation.MaterialBatchId == item.MaterialBatchId
                    && reservation.WarehouseLocationShelfId == item.WarehouseLocationShelfId))
                .ThenBy(item => item.MaterialBatch.ReturnDate == null).ThenBy(item => item.MaterialBatch.ReturnDate)
                .ThenBy(item => MaterialValidity.EffectiveDate(item.MaterialBatch)).ThenBy(item => item.Id).ToList();
            foreach (var shelf in candidates)
            {
                if (remaining <= 0) break;
                if (shelf.UoMId != shelf.MaterialBatch.UoMId || shelf.MaterialBatch.UoMId != requirement.UnitId)
                { blockers.Add($"{requirement.Name}: shelf, batch and department stock units must agree. Reconcile the stock units first."); continue; }
                var available = UomConverter.Convert(shelf.Quantity, shelf.MaterialBatch.UoM?.Symbol, requirement.Unit);
                var held = others.Where(item => item.MaterialBatchId == shelf.MaterialBatchId && item.WarehouseLocationShelfId == shelf.WarehouseLocationShelfId)
                    .Select(item => UomConverter.Convert(item.Quantity, item.UoM?.Symbol, requirement.Unit)).ToList();
                if (!available.HasValue || held.Any(quantity => !quantity.HasValue))
                { blockers.Add($"{requirement.Name}: existing stock/reservation units cannot be converted safely."); continue; }
                var quantity = Math.Min(remaining, Math.Max(0, available.Value - held.Sum(value => value.GetValueOrDefault())));
                if (quantity <= 0) continue;
                var physical = UomConverter.Convert(quantity, requirement.Unit, shelf.MaterialBatch.UoM.Symbol);
                if (!physical.HasValue) continue;
                allocations.Add(new ProductionMaterialAllocation(shelf.MaterialBatchId, shelf.WarehouseLocationShelfId, shelf.MaterialBatch.UoMId, physical.Value));
                remaining -= quantity;
            }
            shortages[requirement.MaterialId] = remaining;
        }
        var evidence = ProductionMaterialSources.Hash(new
        {
            Shelves = shelves.OrderBy(item => item.Id).Select(item => new { item.Id, item.MaterialBatchId, item.WarehouseLocationShelfId,
                item.UoMId, item.Quantity, item.MaterialBatch.Status, item.MaterialBatch.ExpiryDate, item.MaterialBatch.RetestDate, item.MaterialBatch.Material.Kind, item.MaterialBatch.ReturnDate, item.MaterialBatch.UoM.Symbol }),
            Others = others.OrderBy(item => item.Id).Select(item => new { item.Id, item.MaterialBatchId, item.WarehouseLocationShelfId, item.UoMId, item.Quantity, item.UoM.Symbol })
        });
        return new ProductionMaterialAllocationPlan(allocations, shortages, evidence, blockers.Distinct().ToList());
    }

    internal static void Apply(ApplicationDbContext context, ProductionMaterialSource source,
        List<MaterialBatchReservedQuantity> existing, List<ProductionMaterialAllocation> desired)
    {
        var pending = desired.ToDictionary(item => (item.BatchId, item.ShelfId, item.UnitId));
        foreach (var row in existing)
        {
            var key = (row.MaterialBatchId, row.WarehouseLocationShelfId.GetValueOrDefault(), row.UoMId);
            if (pending.Remove(key, out var target)) row.Quantity = target.Quantity;
            else row.ReleasedAt = DateTime.UtcNow;
        }
        foreach (var row in pending.Values)
            context.MaterialBatchReservedQuantities.Add(new MaterialBatchReservedQuantity
            {
                ProductionScheduleProductId = source.Batch.Id, WarehouseId = source.ProductionId,
                MaterialBatchId = row.BatchId, WarehouseLocationShelfId = row.ShelfId, UoMId = row.UnitId, Quantity = row.Quantity
            });
    }
}
