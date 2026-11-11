using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Repository;

internal static class StockTransferPicking
{
    internal sealed record AvailableShelf(ShelfMaterialBatch Shelf, decimal Quantity);

    internal static async Task<List<AvailableShelf>> Load(
        ApplicationDbContext context, Guid materialId, Guid warehouseId, Guid unitId)
    {
        var now = DateTime.UtcNow;
        var shelves = await context.ShelfMaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Include(row => row.MaterialBatch).ThenInclude(batch => batch.UoM)
            .Include(row => row.WarehouseLocationShelf).ThenInclude(shelf => shelf.WarehouseLocationRack)
                .ThenInclude(rack => rack.WarehouseLocation)
            .Where(row => row.DeletedAt == null && row.Quantity > 0
                && row.MaterialBatch.MaterialId == materialId && row.MaterialBatch.DeletedAt == null
                && row.MaterialBatch.Status == BatchStatus.Available
                && (row.UoMId == null || row.UoMId == row.MaterialBatch.UoMId)
                && row.MaterialBatch.UoM.DeletedAt == null
                && row.MaterialBatch.UoMId == unitId
                && row.WarehouseLocationShelf.DeletedAt == null
                && row.WarehouseLocationShelf.WarehouseLocationRack.DeletedAt == null
                && row.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.DeletedAt == null
                && row.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId == warehouseId)
            .WhereMaterialDate(row => row.MaterialBatch, date => date == null || date == DateTime.MinValue || date > now)
            .OrderBy(row => row.MaterialBatch.ReturnDate == null).ThenBy(row => row.MaterialBatch.ReturnDate)
            .ThenBy(MaterialValidity.DateFor<ShelfMaterialBatch>(row => row.MaterialBatch))
            .ThenBy(row => row.MaterialBatchId).ThenBy(row => row.Id)
            .ToListAsync();

        var batchIds = shelves.Select(row => row.MaterialBatchId).Distinct().ToList();
        if (batchIds.Count == 0) return [];
        var holds = (await ProductionMaterialAllocator.UnissuedReservations(context)
            .Where(row => batchIds.Contains(row.MaterialBatchId))
            .Select(row => new { row.MaterialBatchId, row.WarehouseLocationShelfId, row.UoMId, row.Quantity })
            .ToListAsync()).ToLookup(row => row.MaterialBatchId);

        var available = new List<AvailableShelf>();
        foreach (var batch in shelves.GroupBy(row => row.MaterialBatchId))
        {
            var reservations = holds[batch.Key];
            var stockUnitId = batch.First().MaterialBatch.UoMId;
            if (reservations.Any(row => row.UoMId != stockUnitId)) continue;
            var unlocated = reservations.Where(row => !row.WarehouseLocationShelfId.HasValue).Sum(row => row.Quantity);
            var heldByShelf = reservations.Where(row => row.WarehouseLocationShelfId.HasValue)
                .GroupBy(row => row.WarehouseLocationShelfId.GetValueOrDefault())
                .ToDictionary(group => group.Key, group => group.Sum(row => row.Quantity));
            foreach (var shelf in batch)
            {
                var shelfId = shelf.WarehouseLocationShelfId;
                var held = Math.Min(shelf.Quantity, heldByShelf.GetValueOrDefault(shelfId));
                heldByShelf[shelfId] = heldByShelf.GetValueOrDefault(shelfId) - held;
                var free = shelf.Quantity - held;
                var retain = Math.Min(free, unlocated);
                unlocated -= retain;
                if (free > retain) available.Add(new AvailableShelf(shelf, free - retain));
            }
        }
        return available;
    }

    internal static List<AvailableShelf> Pick(IEnumerable<AvailableShelf> available, decimal quantity)
    {
        var picked = new List<AvailableShelf>();
        foreach (var entry in available)
        {
            if (quantity <= 0) break;
            var take = Math.Min(entry.Quantity, quantity);
            picked.Add(entry with { Quantity = take });
            quantity -= take;
        }
        return picked;
    }
}
