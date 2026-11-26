using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>
/// A material's bin card in one department's store: the department's store warehouses for the material's kind,
/// every ledger row in date order, and the balance after each row replayed from the rows themselves.
/// Balances saved on the rows are never trusted: a row posted late (for example a backfilled transfer) must move
/// every balance after it.
/// </summary>
internal static class MaterialStoreLedger
{
    internal sealed record Entry(Guid Id, DateTime At, Guid MaterialBatchId, decimal Received, decimal Issued, decimal Balance);

    internal sealed record Ledger(List<Guid> Stores, UnitOfMeasure Unit, decimal? Density, List<Entry> Entries)
    {
        internal decimal Received => Entries.Sum(row => row.Received);
        internal decimal Issued => Entries.Sum(row => row.Issued);
        internal decimal Closing => Entries.Count == 0 ? 0 : Entries[^1].Balance;
    }

    internal static WarehouseType StoreType(MaterialKind kind) =>
        kind == MaterialKind.Raw ? WarehouseType.RawMaterialStorage : WarehouseType.PackagedStorage;

    /// <summary>The department's store warehouses for the kind, retired ones included so their history still counts.</summary>
    internal static Task<List<Guid>> Stores(ApplicationDbContext db, Guid departmentId, MaterialKind kind) =>
        db.Warehouses.IgnoreQueryFilters().AsNoTracking()
            .Where(row => row.DepartmentId == departmentId && row.Type == StoreType(kind)).Select(row => row.Id).ToListAsync();

    /// <summary>The unit the department reports the material in: its department link's unit, else the one unit all lots share.</summary>
    internal static async Task<Result<UnitOfMeasure>> Unit(ApplicationDbContext db, Guid materialId, Guid departmentId)
    {
        var links = await db.MaterialDepartments.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(row => row.MaterialId == materialId && row.DepartmentId == departmentId && row.DeletedAt == null && row.UoM.DeletedAt == null)
            .Select(row => row.UoMId).Distinct().ToListAsync();
        if (links.Count == 1) return await db.UnitOfMeasures.IgnoreQueryFilters().AsNoTracking().SingleAsync(row => row.Id == links[0]);
        var lots = await db.MaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(row => row.MaterialId == materialId).Select(row => row.UoMId).Distinct().ToListAsync();
        if (links.Count == 0 && lots.Count == 1) return await db.UnitOfMeasures.IgnoreQueryFilters().AsNoTracking().SingleAsync(row => row.Id == lots[0]);
        if (links.Count == 0 && lots.Count == 0) return Result.Success<UnitOfMeasure>(null);
        return Error.Conflict("StockLedger.Unit", "Choose one receiving unit on this material's department link before reconciling its stock.");
    }

    internal static async Task<Result<Ledger>> Load(ApplicationDbContext db, Guid materialId, MaterialKind kind, Guid departmentId)
    {
        var unit = await Unit(db, materialId, departmentId);
        if (unit.IsFailure) return unit.Error;
        var stores = await Stores(db, departmentId, kind);
        var density = await MaterialDensity.Load(db, materialId);
        var rows = await db.BinCardInformation.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(row => row.DeletedAt == null && row.MaterialBatchId.HasValue && row.MaterialBatch.MaterialId == materialId
                && row.WarehouseId.HasValue && stores.Contains(row.WarehouseId.Value))
            .OrderBy(row => row.CreatedAt).ThenBy(row => row.Id)
            .Select(row => new { row.Id, row.CreatedAt, MaterialBatchId = row.MaterialBatchId!.Value, row.QuantityReceived, row.QuantityIssued,
                Unit = row.UoMId.HasValue ? row.UoM.Symbol : row.MaterialBatch.UoM.Symbol })
            .ToListAsync();
        var entries = new List<Entry>(rows.Count);
        var balance = 0m;
        foreach (var row in rows)
        {
            var received = Convert(row.QuantityReceived, row.Unit, unit.Value?.Symbol, density);
            var issued = Convert(row.QuantityIssued, row.Unit, unit.Value?.Symbol, density);
            if (received is null || issued is null)
                return Error.Conflict("StockLedger.Unit", "Some bin card rows cannot be converted to this department's unit. Record the material's verified density or reconcile the units.");
            balance += received.Value - issued.Value;
            entries.Add(new(row.Id, row.CreatedAt, row.MaterialBatchId, received.Value, issued.Value, balance));
        }
        return new Ledger(stores, unit.Value, density, entries);
    }

    private static decimal? Convert(decimal quantity, string? from, string to, decimal? density) =>
        quantity == 0 ? 0 : MaterialDensity.Convert(quantity, from, to, density);
}
