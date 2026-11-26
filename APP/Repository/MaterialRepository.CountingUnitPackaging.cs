using DOMAIN.Entities.Materials;
using Microsoft.EntityFrameworkCore;

namespace APP.Repository;

public partial class MaterialRepository
{
    private async Task<(List<CountingUnitRecord> Records, List<Action> Writes)> PlanCountingUnitPackaging(
        Guid materialId, Guid departmentId, string from, string target, Guid actor,
        Func<string, decimal, string, string, CountingUnitValue> convert, bool track)
    {
        var query = context.ProductPackages.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Include(package => package.Product).Include(package => package.Substitutes)
            .Where(package => package.DeletedAt == null && package.Product.DeletedAt == null
                && package.Product.DepartmentId == departmentId
                && (package.MaterialId == materialId || package.DirectLinkMaterialId == materialId
                    || package.Substitutes.Any(sub => sub.DeletedAt == null && sub.SubstituteMaterialId == materialId)));
        var packages = await (track ? query : query.AsNoTracking()).OrderBy(package => package.Id).ToListAsync();
        var records = new List<CountingUnitRecord>();
        var writes = new List<Action>();
        foreach (var package in packages)
        {
            var values = new List<CountingUnitValue>();
            CountingUnitValue Add(string label, decimal quantity, string beforeUnit, string afterUnit)
            {
                var value = convert(label, quantity, beforeUnit, afterUnit);
                values.Add(value);
                return value;
            }

            var main = package.MaterialId == materialId;
            var referenced = package.DirectLinkMaterialId == materialId;
            if (main)
            {
                var standard = Add("Base quantity", package.BaseQuantity, from, target);
                var full = Add("Full-batch quantity", package.PrescribedQuantity, from, target);
                var loose = Add("Loose quantity", package.Loose, from, target);
                writes.Add(() =>
                {
                    package.BaseQuantity = standard.After;
                    package.PrescribedQuantity = full.After;
                    package.Loose = loose.After;
                    package.LastUpdatedById = actor;
                });
            }
            if (main != referenced && package.DirectLinkMaterialId.HasValue && package.UnitCapacity != 0)
            {
                // Capacity measures linked units per unit of the package. A change to its denominator scales inversely.
                var capacity = referenced
                    ? Add("Linked units per package unit", package.UnitCapacity, from, target)
                    : Add("Linked units per package unit", package.UnitCapacity, target, from);
                writes.Add(() =>
                {
                    package.UnitCapacity = capacity.After;
                    package.LastUpdatedById = actor;
                });
            }
            foreach (var substitute in package.Substitutes.Where(sub => sub.DeletedAt == null).OrderBy(sub => sub.Id))
            {
                if (substitute.SubstituteMaterialId == materialId)
                {
                    var value = Add("Substitute full-batch quantity", substitute.PrescribedQuantity ?? package.PrescribedQuantity, from, target);
                    writes.Add(() =>
                    {
                        substitute.PrescribedQuantity = value.After;
                        substitute.LastUpdatedById = actor;
                    });
                }
                else if (main && substitute.PrescribedQuantity is null && package.PrescribedQuantity != 0)
                {
                    // Preserve the substitute's previous one-for-one amount before the main quantity changes.
                    var previous = package.PrescribedQuantity;
                    values.Add(new CountingUnitValue { Label = "Inherited substitute full-batch quantity", Before = previous, After = previous });
                    writes.Add(() =>
                    {
                        substitute.PrescribedQuantity = previous;
                        substitute.LastUpdatedById = actor;
                    });
                }
            }
            if (values.Count > 0)
                records.Add(new CountingUnitRecord { Id = package.Id, Reference = package.Product.Name, Values = values });
        }
        return (records, writes);
    }
}
