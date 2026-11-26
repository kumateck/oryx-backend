using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials.Request;
using DOMAIN.Entities.Materials;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>Each formula option is validated against its own department link and stored in mg, mL or its counted unit.</summary>
internal static class BomMaterialUnits
{
    internal static async Task<Result> Normalize(ApplicationDbContext db, List<CreateBoMItemsRequest> items, List<MaterialDepartment> links)
    {
        var units = await db.UnitOfMeasures.IgnoreQueryFilters().AsNoTracking().Where(unit => unit.DeletedAt == null).ToListAsync();
        Result<(UnitOfMeasure Unit, decimal Base, decimal Full)> Convert(Guid materialId, Guid? sourceId, decimal perUnit, decimal full, bool density)
        {
            var matches = links.Where(link => link.MaterialId == materialId && link.Material is { DeletedAt: null }
                && link.UoM is { DeletedAt: null }).ToList();
            if (matches.Count != 1) return Error.Conflict("Bom.MaterialUnit", "A BOM material and each substitute need exactly one active department stock-unit link.");
            var link = matches[0];
            if (perUnit <= 0 || full <= 0) return Error.Validation("Bom.Quantity", $"{link.Material.Name}: enter positive unit and full-batch quantities.");
            if (density)
            {
                if (MaterialDensity.ConvertBomMass(1m, "mg", link) is null)
                    return Error.Conflict("Bom.Density", $"{link.Material.Name}: count it by mass, or by volume with a positive measured density in an active g/mL unit, before enabling conversion.");
                var milligrams = units.Where(unit => unit.Type == link.UoM.Type && unit.Symbol.Trim().Equals("mg", StringComparison.OrdinalIgnoreCase)).ToList();
                return milligrams.Count == 1 ? (milligrams[0], perUnit, full)
                    : Error.Conflict("Bom.DensityUnit", "The canonical mg unit must exist exactly once before density conversion can be enabled.");
            }
            var source = units.SingleOrDefault(unit => unit.Id == sourceId);
            var symbol = link.UoM.Symbol.Trim().ToLowerInvariant();
            var smallest = symbol is "mg" or "g" or "kg" ? "mg" : symbol is "ml" or "l" ? "ml" : null;
            if (source is null || source.Type != link.UoM.Type || UomConverter.Convert(1m, source.Symbol, link.UoM.Symbol) is null
                || smallest is null && source.Id != link.UoMId)
                return Error.Conflict("Bom.MaterialUnit", $"{link.Material.Name}: choose a unit compatible with its department material linkage ({link.UoM.Symbol}) and review the quantity.");
            var targets = smallest is null ? [link.UoM] : units.Where(unit => unit.Type == link.UoM.Type
                && unit.Symbol.Trim().Equals(smallest, StringComparison.OrdinalIgnoreCase)).ToList();
            if (targets.Count != 1) return Error.Conflict("Bom.StorageUnit", $"Configure exactly one active {smallest} unit before saving this BOM.");
            var target = targets[0];
            try
            {
                var unitQuantity = UomConverter.Convert(perUnit, source.Symbol, target.Symbol);
                var batchQuantity = UomConverter.Convert(full, source.Symbol, target.Symbol);
                return unitQuantity is > 0 && batchQuantity is > 0 ? (target, unitQuantity.Value, batchQuantity.Value)
                    : Error.Validation("Bom.Quantity", $"{link.Material.Name}: the converted quantities must be greater than zero.");
            }
            catch (OverflowException) { return Error.Validation("Bom.Quantity", $"{link.Material.Name}: the converted quantity is too large."); }
        }

        foreach (var item in items)
        {
            var main = Convert(item.MaterialId, item.BaseUoMId, item.BaseQuantity, item.PrescribedQuantity, item.UseDensityConversion);
            if (main.IsFailure) return main.Error;
            item.BaseUoMId = main.Value.Unit.Id;
            item.BaseQuantity = main.Value.Base;
            item.PrescribedQuantity = main.Value.Full;
            foreach (var substitute in item.Substitutes)
            {
                var converted = Convert(substitute.MaterialId, substitute.BaseUoMId, substitute.BaseQuantity, substitute.PrescribedQuantity, item.UseDensityConversion);
                if (converted.IsFailure) return converted.Error;
                substitute.BaseUoMId = converted.Value.Unit.Id;
                substitute.BaseQuantity = converted.Value.Base;
                substitute.PrescribedQuantity = converted.Value.Full;
            }
            // Legacy id-only substitutes still need their link and density checked, even without quantities.
            foreach (var id in item.SubstituteMaterialIds.Except(item.Substitutes.Select(sub => sub.MaterialId)))
            {
                var link = links.FirstOrDefault(link => link.MaterialId == id);
                var check = Convert(id, link?.UoMId, 1m, 1m, item.UseDensityConversion);
                if (check.IsFailure) return check.Error;
            }
        }
        return Result.Success();
    }
}
