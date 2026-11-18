using DOMAIN.Entities.Materials.Batch;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Repository;

/// <summary>
/// One material's stock in one store, by the rule <see cref="ProductionMaterialAllocator.Plan"/> applies when a batch
/// starts. Every screen that says how much is free (approved materials, the schedule forecast) reads it from here.
/// <list type="bullet">
/// <item><c>OnHand</c>: everything on the store's shelves.</item>
/// <item><c>Expired</c>: lots past their expiry or retest date.</item>
/// <item><c>Unavailable</c>: lots not released for use (quarantined, frozen, rejected), or whose shelf unit cannot be read.</item>
/// <item><c>Usable</c>: approved and in date. Only this can be issued to production.</item>
/// <item><c>Held</c>: usable stock held for batches whose stock has not been issued yet. A hold larger than its shelf
/// counts only up to the shelf: the rest has already left the store.</item>
/// <item><c>Free</c>: usable less held. What the next batch can reserve.</item>
/// </list>
/// Quantities are in the unit given for the material (the department's stock unit), converted with its density.
/// </summary>
internal sealed record StoreStockLevel(decimal OnHand, decimal Expired, decimal Unavailable, decimal Usable, decimal Held, decimal Free)
{
    internal static readonly StoreStockLevel None = new(0, 0, 0, 0, 0, 0);
}

internal static class StoreStock
{
    /// <summary>Measures the materials in a store, each in the store department's stock unit for it.</summary>
    internal static async Task<Dictionary<Guid, StoreStockLevel>> MeasureInStore(ApplicationDbContext context, Guid storeId,
        IReadOnlyCollection<Guid> materialIds, CancellationToken cancellationToken = default)
    {
        var departmentId = await context.Warehouses.IgnoreQueryFilters().Where(w => w.Id == storeId)
            .Select(w => w.DepartmentId).SingleOrDefaultAsync(cancellationToken);
        var links = await context.MaterialDepartments.IgnoreQueryFilters().AsNoTracking()
            .Include(link => link.UoM).Include(link => link.DensityUoM).Include(link => link.Material)
            .Where(link => materialIds.Contains(link.MaterialId) && link.DepartmentId == departmentId && link.DeletedAt == null)
            .ToListAsync(cancellationToken);
        var units = materialIds.Distinct().ToDictionary(id => id, id =>
        {
            // Several links for one material are ambiguous: measure in the lots' own unit rather than guess.
            var matching = links.Where(link => link.MaterialId == id).ToList();
            var link = matching.Count == 1 && matching[0].UoM is { DeletedAt: null } ? matching[0] : null;
            return (link?.UoM?.Symbol, link is null ? (decimal?)null : MaterialDensity.Verified(link));
        });
        return await Measure(context, storeId, units, cancellationToken);
    }

    /// <param name="units">Each material's display unit and density. A material without one is measured in its lots' own unit.</param>
    internal static async Task<Dictionary<Guid, StoreStockLevel>> Measure(ApplicationDbContext context, Guid storeId,
        IReadOnlyDictionary<Guid, (string? Unit, decimal? Density)> units, CancellationToken cancellationToken = default)
    {
        var ids = units.Keys.ToList();
        var shelves = await context.ShelfMaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Include(item => item.MaterialBatch).ThenInclude(item => item.UoM)
            .Where(item => ids.Contains(item.MaterialBatch.MaterialId) && item.DeletedAt == null && item.MaterialBatch.DeletedAt == null
                && item.Quantity > 0
                && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId == storeId
                && item.WarehouseLocationShelf.DeletedAt == null && item.WarehouseLocationShelf.WarehouseLocationRack.DeletedAt == null
                && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.DeletedAt == null)
            .ToListAsync(cancellationToken);
        var shelfIds = shelves.Select(item => item.WarehouseLocationShelfId).Distinct().ToList();
        var holds = (await ProductionMaterialAllocator.UnissuedReservations(context).IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.WarehouseLocationShelfId.HasValue && shelfIds.Contains(item.WarehouseLocationShelfId.Value))
            .Select(item => new { item.MaterialBatchId, ShelfId = item.WarehouseLocationShelfId!.Value, item.Quantity, Unit = item.UoM.Symbol })
            .ToListAsync(cancellationToken)).ToLookup(item => (item.MaterialBatchId, item.ShelfId));
        // Older holds recorded no shelf. They still sit on their lot: take them from the lot's free stock in this store,
        // as a stock transfer does, so that stock is never counted free twice.
        var lotIds = shelves.Select(item => item.MaterialBatchId).Distinct().ToList();
        var unlocated = (await ProductionMaterialAllocator.UnissuedReservations(context).IgnoreAutoIncludes().AsNoTracking()
            .Where(item => !item.WarehouseLocationShelfId.HasValue && lotIds.Contains(item.MaterialBatchId))
            .Select(item => new { item.MaterialBatchId, item.Quantity, Unit = item.UoM.Symbol })
            .ToListAsync(cancellationToken)).ToLookup(item => item.MaterialBatchId);
        var lotFree = new Dictionary<Guid, (Guid MaterialId, decimal Free, string? Unit, decimal? Density)>();

        var now = DateTime.UtcNow;
        var result = new Dictionary<Guid, StoreStockLevel>();
        foreach (var shelf in shelves)
        {
            var materialId = shelf.MaterialBatch.MaterialId;
            var (unit, density) = units[materialId];
            var lotUnit = shelf.MaterialBatch.UoM?.Symbol;
            var target = unit ?? lotUnit;
            var level = result.GetValueOrDefault(materialId, StoreStockLevel.None);
            var quantity = MaterialDensity.Convert(shelf.Quantity, lotUnit, target, density);
            // Stock whose unit cannot be read, or whose shelf and lot units disagree, cannot be issued (the allocator refuses it).
            if (quantity is null || shelf.UoMId != shelf.MaterialBatch.UoMId)
            {
                result[materialId] = level with { OnHand = level.OnHand + (quantity ?? 0), Unavailable = level.Unavailable + (quantity ?? 0) };
                continue;
            }
            level = level with { OnHand = level.OnHand + quantity.Value };
            if (MaterialValidity.IsExpired(shelf.MaterialBatch, now))
                level = level with { Expired = level.Expired + quantity.Value };
            else if (shelf.MaterialBatch.Status != BatchStatus.Available)
                level = level with { Unavailable = level.Unavailable + quantity.Value };
            else
            {
                var held = holds[(shelf.MaterialBatchId, shelf.WarehouseLocationShelfId)]
                    .Select(item => MaterialDensity.Convert(item.Quantity, item.Unit, target, density)).ToList();
                // A hold in a unit that cannot be converted keeps the whole shelf, as the allocator refuses to take from it.
                var heldHere = held.Any(value => value is null) ? quantity.Value : Math.Min(quantity.Value, held.Sum(value => value.GetValueOrDefault()));
                level = level with { Usable = level.Usable + quantity.Value, Held = level.Held + heldHere, Free = level.Free + quantity.Value - heldHere };
                var lot = lotFree.TryGetValue(shelf.MaterialBatchId, out var seen) ? seen : (MaterialId: materialId, Free: 0m, Unit: target, Density: density);
                lotFree[shelf.MaterialBatchId] = lot with { Free = lot.Free + quantity.Value - heldHere };
            }
            result[materialId] = level;
        }
        foreach (var (lotId, lot) in lotFree)
        {
            var held = unlocated[lotId].Select(item => MaterialDensity.Convert(item.Quantity, item.Unit, lot.Unit, lot.Density)).ToList();
            if (held.Count == 0) continue;
            var take = held.Any(value => value is null) ? lot.Free : Math.Min(lot.Free, held.Sum(value => value.GetValueOrDefault()));
            var level = result[lot.MaterialId];
            result[lot.MaterialId] = level with { Held = level.Held + take, Free = level.Free - take };
        }
        return result;
    }
}

internal static class StoreStockDtos
{
    /// <summary>Fills a material row from the store-stock rule: on hand, expired, unavailable, held and free.</summary>
    internal static DOMAIN.Entities.Materials.MaterialDetailsDto WithStock(this DOMAIN.Entities.Materials.MaterialDetailsDto row, StoreStockLevel stock)
    {
        row.TotalAvailableQuantity = stock.OnHand;
        row.ExpiredQuantity = stock.Expired;
        row.UnavailableQuantity = stock.Unavailable;
        row.ReservedQuantity = stock.Held;
        row.FreeQuantity = stock.Free;
        return row;
    }

    /// <summary>Adds one store's stock to another's, for totals over several stores.</summary>
    internal static StoreStockLevel Plus(this StoreStockLevel a, StoreStockLevel b) =>
        new(a.OnHand + b.OnHand, a.Expired + b.Expired, a.Unavailable + b.Unavailable, a.Usable + b.Usable, a.Held + b.Held, a.Free + b.Free);
}
