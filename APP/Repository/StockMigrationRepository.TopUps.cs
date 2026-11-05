using DOMAIN.Entities.Materials.StockMigrations;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class StockMigrationRepository
{
    internal static Error ReasonError(string reason) =>
        string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000
            ? Error.Validation("StockMigration.Reason", "Enter a top-up reason of at most 2,000 characters.")
            : null;

    // Receipt references belong to the lot identity in a top-up. Existing lots are never reused.
    internal static (string Batch, Guid UnbatchedLine, string Grn, string Ar) BatchIdentity(
        StockMigration migration, Guid lineId, string batchNumber, string grn, string ar)
    {
        var identity = APP.Services.Materials.OpeningBalanceBatches.Identity(lineId, batchNumber);
        return (identity.Batch, identity.UnbatchedLine,
            migration.Mode == StockMigrationMode.TopUp ? grn?.Trim() ?? string.Empty : string.Empty,
            migration.Mode == StockMigrationMode.TopUp ? ar?.Trim() ?? string.Empty : string.Empty);
    }

    public async Task<Result<Guid>> CreateTopUp(CreateStockTopUpRequest request, Guid userId)
    {
        if (ReasonError(request.Reason) is { } error)
            return error;
        if (request.Lines is not { Count: > 0 and <= 1000 } || request.Lines.Any(l => l == null))
            return Error.Validation("StockMigration.Lines", "Enter between 1 and 1,000 stock rows.");
        if (Unit(request.MassUnit, MassUnits) is not { } mass || Unit(request.VolumeUnit, VolumeUnits) is not { } volume)
            return Error.Validation("StockMigration.Units", "Choose kg, g or mg for weights and L or ml for liquids.");
        if (!await context.Departments.IgnoreQueryFilters().AnyAsync(d => d.Id == request.DepartmentId && d.DeletedAt == null))
            return Error.NotFound("Department.NotFound", "The department was not found.");

        var migration = new StockMigration
        {
            Id = Guid.NewGuid(), DepartmentId = request.DepartmentId,
            Mode = StockMigrationMode.TopUp, Reason = request.Reason.Trim(),
            FileName = "Manual entry", MassUnit = mass, VolumeUnit = volume,
            Status = StockMigrationStatus.Draft, CreatedById = userId,
            Lines = request.Lines.Select((line, index) => new StockMigrationLine
            {
                Id = Guid.NewGuid(), Sheet = "Manual entry", RowNumber = index + 1,
                MaterialCode = line.MaterialCode?.Trim().ToUpperInvariant(), MaterialId = line.MaterialId,
                SheetMaterialCode = line.MaterialCode, SheetWarehouse = line.WarehouseName,
                SheetShelf = line.ShelfName, SheetBatchNumber = line.BatchNumber,
                SheetUnit = line.UnitSymbol,
                SheetQuantity = line.QuantityInSheet?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                SheetManufacturingDate = line.ManufacturingDate?.ToString(request.ExactDates ? "yyyy-MM-dd" : "yyyy-MM"),
                SheetExpiryDate = line.ExpiryDate?.ToString(request.ExactDates ? "yyyy-MM-dd" : "yyyy-MM"),
                WarehouseName = line.WarehouseName?.Trim(), ShelfName = line.ShelfName?.Trim(), ShelfId = line.ShelfId,
                BatchNumber = line.BatchNumber?.Trim(), UnitSymbol = line.UnitSymbol?.Trim(),
                QuantityInSheet = line.QuantityInSheet is > 0 ? line.QuantityInSheet : null,
                ManufacturingDate = Date(line.ManufacturingDate, request.ExactDates),
                ExpiryDate = Date(line.ExpiryDate, request.ExactDates),
                RetestDate = Day(line.RetestDate), ExactDates = request.ExactDates,
                Grn = line.Grn?.Trim(), ArNumber = line.ArNumber?.Trim(), Excluded = line.Excluded,
            }).ToList(),
        };
        // Unresolved rows may be corrected in the saved draft, exactly as for workbook uploads.
        await Check(migration);
        context.StockMigrations.Add(migration);
        await context.SaveChangesAsync();
        return migration.Id;
    }

    /// <summary>
    /// A top-up adds new lots, so a batch number already in stock, or already waiting in another top-up
    /// draft, may be the same stock entered twice. The person checking decides; it is not refused.
    /// </summary>
    private async Task WarnRepeatedTopUpBatches(StockMigration migration)
    {
        var numbered = migration.Lines.Where(l => l.ResolvedMaterialId.HasValue && !string.IsNullOrWhiteSpace(l.BatchNumber)
            && l.Status is StockMigrationLineStatus.Ok or StockMigrationLineStatus.Warning).ToList();
        if (numbered.Count == 0) return;
        var materials = numbered.Select(l => l.ResolvedMaterialId!.Value).Distinct().ToList();
        var numbers = numbered.Select(l => l.BatchNumber.Trim().ToUpperInvariant()).Distinct().ToList();
        var inStock = (await context.MaterialBatches.AsNoTracking().IgnoreAutoIncludes()
                .Where(b => materials.Contains(b.MaterialId) && numbers.Contains(b.BatchNumber.Trim().ToUpper()))
                .Select(b => new { b.MaterialId, Number = b.BatchNumber.Trim().ToUpper() }).ToListAsync())
            .Select(b => (b.MaterialId, b.Number)).ToHashSet();
        var drafted = (await context.StockMigrationLines.AsNoTracking().IgnoreAutoIncludes()
                .Where(l => l.StockMigrationId != migration.Id && !l.Excluded
                    && l.StockMigration.Mode == StockMigrationMode.TopUp && l.StockMigration.Status == StockMigrationStatus.Draft
                    && l.StockMigration.DeletedAt == null && l.ResolvedMaterialId != null
                    && materials.Contains(l.ResolvedMaterialId.Value) && numbers.Contains(l.BatchNumber.Trim().ToUpper()))
                .Select(l => new { Material = l.ResolvedMaterialId.Value, Number = l.BatchNumber.Trim().ToUpper() }).ToListAsync())
            .Select(l => (l.Material, l.Number)).ToHashSet();
        foreach (var line in numbered)
        {
            var key = (line.ResolvedMaterialId!.Value, line.BatchNumber.Trim().ToUpperInvariant());
            var notes = new List<string>();
            if (inStock.Contains(key))
                notes.Add($"Batch {line.BatchNumber.Trim()} of this material is already in stock. Check this is new stock and not entered twice; approval adds it as a separate lot.");
            if (drafted.Contains(key))
                notes.Add($"Batch {line.BatchNumber.Trim()} of this material is also in another top-up draft waiting for approval.");
            if (notes.Count == 0) continue;
            line.Warnings = Join([.. Split(line.Warnings), .. notes]);
            if (line.Status == StockMigrationLineStatus.Ok) line.Status = StockMigrationLineStatus.Warning;
        }
    }

    private async Task SnapshotTopUp(StockMigration migration)
    {
        var comparison = await Comparison(migration.Id);
        foreach (var row in comparison.Value)
            context.StockMigrationBalances.Add(new StockMigrationBalance
            {
                StockMigrationId = migration.Id, MaterialId = row.MaterialId, Unit = row.Unit,
                Before = row.Current, Added = row.Added,
                BeforeBatches = row.CurrentBatches, AddedBatches = row.NewBatches - row.CurrentBatches,
            });
    }

    private async Task<Dictionary<(Guid Material, Guid Warehouse), decimal>> ReceiptBalances(List<Guid> materials, List<Guid> stores)
    {
        var balances = await context.BinCardInformation.IgnoreQueryFilters()
            .Where(b => b.MaterialBatchId != null && b.WarehouseId != null
                && materials.Contains(b.MaterialBatch.MaterialId) && stores.Contains(b.WarehouseId.Value))
            .GroupBy(b => new { b.MaterialBatch.MaterialId, b.WarehouseId })
            .Select(g => new { g.Key.MaterialId, WarehouseId = g.Key.WarehouseId.Value,
                Balance = g.Sum(b => b.QuantityReceived - b.QuantityIssued) }).ToListAsync();
        return balances.ToDictionary(b => (b.MaterialId, b.WarehouseId), b => b.Balance);
    }

    private async Task<List<StockMigrationComparisonDto>> ApprovedTopUpComparison(Guid id) =>
        await context.StockMigrationBalances.AsNoTracking().Where(b => b.StockMigrationId == id)
            .OrderBy(b => b.Material.Code)
            .Select(b => new StockMigrationComparisonDto
            {
                MaterialId = b.MaterialId, Code = b.Material.Code, Name = b.Material.Name,
                Kind = b.Material.Kind, Unit = b.Unit, Current = b.Before,
                Added = b.Added, New = b.Before + b.Added,
                CurrentBatches = b.BeforeBatches, NewBatches = b.BeforeBatches + b.AddedBatches,
            }).ToListAsync();
}
