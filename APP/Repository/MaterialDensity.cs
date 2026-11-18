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

    internal static decimal? Convert(decimal quantity, string? from, string? to, decimal? density)
    {
        try { return UomConverter.Convert(quantity, from, to) ?? (density is > 0 ? UomConverter.ConvertWithDensity(quantity, from, to, density.Value) : null); }
        catch (OverflowException) { return null; }
    }

    internal static decimal? ConvertBomMass(decimal milligrams, string? sourceUnit, MaterialDepartment link) =>
        string.Equals(sourceUnit?.Trim(), "mg", StringComparison.OrdinalIgnoreCase) && link.UoM is { DeletedAt: null } unit
            ? Verified(link) is decimal density ? UomConverter.ConvertWithDensity(milligrams, "mg", unit.Symbol, density) : null : null;

    internal static async Task<decimal?> Load(ApplicationDbContext db, Guid materialId) =>
        await db.Materials.IgnoreQueryFilters().AsNoTracking().Where(m => m.Id == materialId && m.DeletedAt == null)
            .Select(m => m.DensityGramsPerMilliliter).SingleOrDefaultAsync();
}
