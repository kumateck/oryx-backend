using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class MaterialDensity
{
    internal static decimal? Verified(MaterialDepartment link) => link.Material?.DensityGramsPerMilliliter is > 0
        ? link.Material.DensityGramsPerMilliliter
        : link.Density > 0 && link.DensityUoMId.HasValue && link.DensityUoM is { DeletedAt: null } unit
            && string.Equals(unit.Symbol?.Trim(), "g/mL", StringComparison.OrdinalIgnoreCase) ? link.Density : null;

    internal static string StockLevelUnit(string symbol) => symbol?.Trim().ToLowerInvariant() switch
    {
        "mg" or "g" or "kg" => "mg",
        "ml" or "l" => "ml",
        _ => symbol,
    };

    internal static decimal? Convert(decimal quantity, string? from, string? to, decimal? density)
    {
        try { return UomConverter.Convert(quantity, from, to) ?? (density is > 0 ? UomConverter.ConvertWithDensity(quantity, from, to, density.Value) : null); }
        catch (OverflowException) { return null; }
    }

    /// <summary>
    /// A BOM line marked for density conversion is a mass in mg. A department that counts the material by mass takes it
    /// as it is (mg to its own mass unit, no density involved); one that counts it by volume needs the verified density.
    /// Null when the line is not in mg, the department unit is neither, or a volume unit has no verified density.
    /// </summary>
    internal static decimal? ConvertBomMass(decimal milligrams, string? sourceUnit, MaterialDepartment link)
    {
        if (!string.Equals(sourceUnit?.Trim(), "mg", StringComparison.OrdinalIgnoreCase) || link.UoM is not { DeletedAt: null } unit) return null;
        try
        {
            return UomConverter.Convert(milligrams, "mg", unit.Symbol)
                ?? (Verified(link) is decimal density ? UomConverter.ConvertWithDensity(milligrams, "mg", unit.Symbol, density) : null);
        }
        catch (OverflowException) { return null; }
    }

    internal static decimal? ConvertBomQuantity(decimal quantity, string sourceUnit, MaterialDepartment link, bool specifiedInMilligrams) =>
        specifiedInMilligrams ? ConvertBomMass(quantity, sourceUnit, link)
        : link.UoM is { DeletedAt: null } unit ? Convert(quantity, sourceUnit, unit.Symbol, Verified(link)) : null;

    internal static async Task<decimal?> Load(ApplicationDbContext db, Guid materialId) =>
        await db.Materials.IgnoreQueryFilters().AsNoTracking().Where(m => m.Id == materialId && m.DeletedAt == null)
            .Select(m => m.DensityGramsPerMilliliter).SingleOrDefaultAsync();
}
