using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using SHARED;

namespace APP.Repository;

internal static class MaterialDensity
{
    private static bool IsGramsPerMilliliter(UnitOfMeasure? unit) =>
        unit is { DeletedAt: null } && string.Equals(unit.Symbol?.Trim(), "g/mL", StringComparison.OrdinalIgnoreCase);

    internal static decimal? ConvertBomMass(decimal milligrams, string? sourceUnit, MaterialDepartment link)
    {
        if (!string.Equals(sourceUnit?.Trim(), "mg", StringComparison.OrdinalIgnoreCase) ||
            !link.DensityUoMId.HasValue || !IsGramsPerMilliliter(link.DensityUoM) ||
            link.UoM is not { DeletedAt: null } stockUnit)
            return null;

        try { return UomConverter.ConvertWithDensity(milligrams, "mg", stockUnit.Symbol, link.Density); }
        catch (OverflowException) { return null; }
    }
}
