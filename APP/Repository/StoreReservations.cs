using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Repository;

/// <summary>
/// Production holds on a store's stock, counted the way the allocator and the forecast count them.
/// A hold records the production warehouse the stock is going to (its <c>WarehouseId</c>) and the store shelf it
/// is taken from, so a store's holds are found by shelf, not by <c>WarehouseId</c>. Holds already issued are left out.
/// </summary>
internal static class StoreReservations
{
    internal static IQueryable<MaterialBatchReservedQuantity> HeldIn(ApplicationDbContext context, Warehouse store) =>
        ProductionMaterialAllocator.UnissuedReservations(context).Where(r =>
            r.WarehouseLocationShelfId.HasValue
                ? r.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId == store.Id
                // A hold without a shelf still sits on a lot in the store: count it when the lot is shelved there.
                : r.WarehouseId == store.Id || context.ShelfMaterialBatches.IgnoreQueryFilters().Any(shelf => shelf.MaterialBatchId == r.MaterialBatchId
                    && shelf.DeletedAt == null && shelf.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId == store.Id));
}
