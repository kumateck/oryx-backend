using System.Globalization;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Repository;

/// <summary>
/// A transfer is requested in the receiving department's unit (for example L), while the supplying store
/// keeps the same material in its own unit (for example kg). The supplying store reads, checks and issues
/// the transfer in its own unit; the request itself is unchanged.
/// </summary>
internal static class StockTransferSupplyUnits
{
    /// <summary>The supplying department's single active stock unit for the material, or null when there is none or several.</summary>
    internal static async Task<UnitOfMeasure?> SupplierUnit(ApplicationDbContext db, Guid materialId, Guid departmentId)
    {
        var units = await db.MaterialDepartments.IgnoreQueryFilters().AsNoTracking()
            .Where(link => link.MaterialId == materialId && link.DepartmentId == departmentId && link.DeletedAt == null
                && link.UoM.DeletedAt == null)
            .Select(link => link.UoM).ToListAsync();
        return units.Count == 1 ? units[0] : null;
    }

    /// <summary>Sets the supplying store's quantity and unit on each row whose request unit converts to it.</summary>
    internal static async Task Apply(ApplicationDbContext db, IEnumerable<DepartmentStockTransferDto?> rows)
    {
        var cache = new Dictionary<(Guid, Guid), (UnitOfMeasure? Unit, decimal? Density)>();
        foreach (var row in rows)
        {
            if (row?.Material is null || row.FromDepartment is null || string.IsNullOrWhiteSpace(row.UoM?.Symbol)) continue;
            var key = (row.Material.Id, row.FromDepartment.Id);
            if (!cache.TryGetValue(key, out var supply))
            {
                supply = (await SupplierUnit(db, key.Item1, key.Item2), await MaterialDensity.Load(db, key.Item1));
                cache[key] = supply;
            }
            if (supply.Unit is null) continue;
            var quantity = MaterialDensity.Convert(row.Quantity, row.UoM.Symbol, supply.Unit.Symbol, supply.Density);
            if (quantity is null) continue;
            row.SupplierQuantity = quantity;
            row.SupplierUoM = new UnitOfMeasureDto { Id = supply.Unit.Id, Name = supply.Unit.Name, Symbol = supply.Unit.Symbol, Type = supply.Unit.Type };
        }
    }

    /// <summary>
    /// The shortfall said in the supplying store's unit, with the quantity asked for in the requester's unit:
    /// "The supplying store has 3,743.3 kg free of the 20,720 kg needed for 20,000 L. It is short by 16,976.7 kg."
    /// </summary>
    internal static string Shortfall(decimal requested, string requestUnit, decimal available, UnitOfMeasure? supplierUnit, decimal? density)
    {
        var unit = supplierUnit?.Symbol;
        var needed = unit is null ? null : MaterialDensity.Convert(requested, requestUnit, unit, density);
        var free = unit is null ? null : MaterialDensity.Convert(available, requestUnit, unit, density);
        if (needed is null || free is null)
            return $"The supplying store has {Readable(available, requestUnit)} free of the {Readable(requested, requestUnit)} asked for. It is short by {Readable(requested - available, requestUnit)}.";
        var asked = string.Equals(unit, requestUnit, StringComparison.OrdinalIgnoreCase) ? "" : $" for {Readable(requested, requestUnit)}";
        return $"The supplying store has {Number(free.Value)} {unit} free of the {Number(needed.Value)} {unit} needed{asked}. It is short by {Number(needed.Value - free.Value)} {unit}.";
    }

    /// <summary>A quantity in the largest metric unit that keeps it at or above one: 20,000,000 mL reads 20,000 L.</summary>
    internal static string Readable(decimal quantity, string? unit)
    {
        var symbol = unit?.Trim() ?? "";
        var (larger, factor) = symbol.ToLowerInvariant() switch
        {
            "ml" => ("L", 1_000m),
            "mg" => (Math.Abs(quantity) >= 1_000_000m ? "kg" : "g", Math.Abs(quantity) >= 1_000_000m ? 1_000_000m : 1_000m),
            "g" => ("kg", 1_000m),
            _ => (null, 1m),
        };
        return larger is not null && Math.Abs(quantity) >= factor
            ? $"{Number(quantity / factor)} {larger}"
            : $"{Number(quantity)} {symbol}".TrimEnd();
    }

    private static string Number(decimal value) => Math.Round(value, 3, MidpointRounding.AwayFromZero).ToString("#,0.###", CultureInfo.InvariantCulture);
}
