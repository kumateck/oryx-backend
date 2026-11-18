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
        IReadOnlyCollection<Guid> materialIds, CancellationToken cancellationToken = default) =>
        await Measure(context, storeId, await UnitsInStore(context, storeId, materialIds, cancellationToken), cancellationToken);

    /// <summary>Each material's stock unit and density in the store's department.</summary>
    internal static async Task<Dictionary<Guid, (string? Unit, decimal? Density)>> UnitsInStore(ApplicationDbContext context, Guid storeId,
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
        return units;
    }

    /// <param name="units">Each material's display unit and density. A material without one is measured in its lots' own unit.</param>
    internal static async Task<Dictionary<Guid, StoreStockLevel>> Measure(ApplicationDbContext context, Guid storeId,
        IReadOnlyDictionary<Guid, (string? Unit, decimal? Density)> units, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, StoreStockLevel>();
        foreach (var shelf in await MeasureShelves(context, storeId, units, cancellationToken))
            result[shelf.MaterialId] = result.GetValueOrDefault(shelf.MaterialId, StoreStockLevel.None).Plus(shelf.Level);
        return result;
    }

    /// <summary>
    /// The same measurement one shelf row at a time, so a material's batch rows add up exactly to its totals.
    /// </summary>
    internal static async Task<List<ShelfStock>> MeasureShelves(ApplicationDbContext context, Guid storeId,
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
            .OrderBy(item => item.MaterialBatchId).ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
        var shelfIds = shelves.Select(item => item.WarehouseLocationShelfId).Distinct().ToList();
        var holds = (await ProductionMaterialAllocator.UnissuedReservations(context).IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.WarehouseLocationShelfId.HasValue && shelfIds.Contains(item.WarehouseLocationShelfId.Value))
            .Select(item => new { item.MaterialBatchId, ShelfId = item.WarehouseLocationShelfId!.Value, item.Quantity, Unit = item.UoM.Symbol })
            .ToListAsync(cancellationToken)).ToLookup(item => (item.MaterialBatchId, item.ShelfId));
        // Older holds recorded no shelf. They still sit on their lot: take them from the lot's free shelves in this store,
        // in order, as a stock transfer does, so that stock is never counted free twice.
        var lotIds = shelves.Select(item => item.MaterialBatchId).Distinct().ToList();
        var unlocated = (await ProductionMaterialAllocator.UnissuedReservations(context).IgnoreAutoIncludes().AsNoTracking()
            .Where(item => !item.WarehouseLocationShelfId.HasValue && lotIds.Contains(item.MaterialBatchId))
            .Select(item => new { item.MaterialBatchId, item.Quantity, Unit = item.UoM.Symbol })
            .ToListAsync(cancellationToken)).ToLookup(item => item.MaterialBatchId);
        var unlocatedLeft = new Dictionary<Guid, decimal?>();

        var now = DateTime.UtcNow;
        var result = new List<ShelfStock>();
        foreach (var shelf in shelves)
        {
            var materialId = shelf.MaterialBatch.MaterialId;
            var (unit, density) = units[materialId];
            var lotUnit = shelf.MaterialBatch.UoM?.Symbol;
            var target = unit ?? lotUnit;
            var quantity = MaterialDensity.Convert(shelf.Quantity, lotUnit, target, density);
            ShelfStock Row(StoreStockState state, StoreStockLevel level) => new(shelf.Id, shelf.MaterialBatchId, materialId, target, state, level);
            // Stock whose unit cannot be read, or whose shelf and lot units disagree, cannot be issued (the allocator refuses it).
            if (quantity is null || shelf.UoMId != shelf.MaterialBatch.UoMId)
            { result.Add(Row(StoreStockState.NotReleased, StoreStockLevel.None with { OnHand = quantity ?? 0, Unavailable = quantity ?? 0 })); continue; }
            if (MaterialValidity.IsExpired(shelf.MaterialBatch, now))
            { result.Add(Row(StoreStockState.Expired, StoreStockLevel.None with { OnHand = quantity.Value, Expired = quantity.Value })); continue; }
            if (shelf.MaterialBatch.Status != BatchStatus.Available)
            { result.Add(Row(StoreStockState.NotReleased, StoreStockLevel.None with { OnHand = quantity.Value, Unavailable = quantity.Value })); continue; }

            var located = holds[(shelf.MaterialBatchId, shelf.WarehouseLocationShelfId)]
                .Select(item => MaterialDensity.Convert(item.Quantity, item.Unit, target, density)).ToList();
            // A hold in a unit that cannot be converted keeps the whole shelf, as the allocator refuses to take from it.
            var held = located.Any(value => value is null) ? quantity.Value : Math.Min(quantity.Value, located.Sum(value => value.GetValueOrDefault()));
            if (!unlocatedLeft.ContainsKey(shelf.MaterialBatchId))
            {
                var lotHolds = unlocated[shelf.MaterialBatchId].Select(item => MaterialDensity.Convert(item.Quantity, item.Unit, target, density)).ToList();
                unlocatedLeft[shelf.MaterialBatchId] = lotHolds.Any(value => value is null) ? null : lotHolds.Sum(value => value.GetValueOrDefault());
            }
            var left = unlocatedLeft[shelf.MaterialBatchId];
            var fromLot = left is null ? quantity.Value - held : Math.Min(quantity.Value - held, left.Value);
            if (left is not null) unlocatedLeft[shelf.MaterialBatchId] = left.Value - fromLot;
            held += fromLot;
            result.Add(Row(StoreStockState.Usable, new StoreStockLevel(quantity.Value, 0, 0, quantity.Value, held, quantity.Value - held)));
        }
        return result;
    }
}

internal enum StoreStockState { Usable, Expired, NotReleased }

/// <summary>One shelf row of one lot, measured in <see cref="Unit"/>.</summary>
internal sealed record ShelfStock(Guid ShelfBatchId, Guid LotId, Guid MaterialId, string? Unit, StoreStockState State, StoreStockLevel Level);

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
