using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal sealed record ProductionMaterialSource(
    ProductionScheduleProduct Batch, Guid DepartmentId, Guid StorageId, Guid ProductionId,
    List<ProductionMaterialRequirement> Requirements, string SourceHash, string DefinitionJson, List<string> Blockers);

internal static class ProductionMaterialSources
{
    internal static string Json<T>(T value) => JsonSerializer.Serialize(value);
    internal static string Hash<T>(T value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Json(value))));
    internal static List<ProductionMaterialRequirement> Requirements(string json) =>
        JsonSerializer.Deserialize<List<ProductionMaterialRequirement>>(json) ?? [];

    internal static async Task<Result> Authorize(ApplicationDbContext context, Guid actorId, Guid batchId, MaterialKind kind)
    {
        if (!Enum.IsDefined(kind)) return Error.Validation("Repair.Kind", "Choose BOM or Packaging.");
        var permission = kind == MaterialKind.Raw ? PermissionKeys.CanRepairBom : PermissionKeys.CanRepairPackaging;
        if (!(await CurrentPermissionSet.Load(context, actorId)).Contains(permission))
            return Error.Forbidden("Repair.Permission", "You do not have permission for this repair.");
        var actor = await context.Users.IgnoreQueryFilters().AsNoTracking().IgnoreAutoIncludes()
            .Where(user => user.Id == actorId && user.DeletedAt == null && !user.IsDisabled)
            .Select(user => new { user.DepartmentId, Type = (DepartmentType?)user.Department.Type }).SingleOrDefaultAsync();
        var batch = await context.ProductionScheduleProducts.IgnoreQueryFilters().AsNoTracking().IgnoreAutoIncludes()
            .Where(item => item.Id == batchId && item.Product.DeletedAt == null && item.ProductionSchedule.DeletedAt == null)
            .Select(item => new { item.Product.DepartmentId, ScheduleDepartment = item.ProductionSchedule.DepartmentId }).SingleOrDefaultAsync();
        if (actor?.Type != DepartmentType.Production || batch is null || !actor.DepartmentId.HasValue
            || actor.DepartmentId != batch.DepartmentId || batch.ScheduleDepartment.HasValue && batch.ScheduleDepartment != actor.DepartmentId)
            return Error.Forbidden("Repair.Department", "This batch must belong to your production department.");
        return Result.Success();
    }

    /// <param name="preferred">For a line nothing is allocated or requested for yet: the option (by main material id) to use instead of the main material.</param>
    internal static async Task<Result<ProductionMaterialSource>> Load(ApplicationDbContext context, Guid batchId, MaterialKind kind,
        Guid? packingStyleOverride = null, IReadOnlyDictionary<Guid, Guid> preferred = null)
    {
        var batch = await context.ProductionScheduleProducts.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Include(item => item.Product).Include(item => item.ProductionSchedule).SingleOrDefaultAsync(item => item.Id == batchId);
        if (batch?.Product is null || batch.Product.DeletedAt.HasValue || batch.ProductionSchedule.DeletedAt.HasValue || batch.Cancelled)
            return Error.Validation("Repair.Batch", "This batch is missing, deleted or cancelled.");
        var departmentId = batch.ProductionSchedule.DepartmentId ?? batch.Product.DepartmentId;
        if (departmentId is null || departmentId != batch.Product.DepartmentId)
            return Error.Validation("Repair.Department", "The batch's product and schedule must have the same production department.");
        var warehouses = await context.Warehouses.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.DepartmentId == departmentId && item.DeletedAt == null).ToListAsync();
        var storage = warehouses.Where(item => item.Type == (kind == MaterialKind.Raw ? WarehouseType.RawMaterialStorage : WarehouseType.PackagedStorage)).ToList();
        var production = warehouses.Where(item => item.Type == WarehouseType.Production).ToList();
        if (storage.Count != 1 || production.Count != 1)
            return Error.Validation("Repair.Warehouse", "Exactly one storage and one production warehouse must be configured for this department.");

        var blockers = new List<string>();
        var definitions = new List<SourceLine>();
        string source;
        if (kind == MaterialKind.Raw)
        {
            var boms = await context.ProductBillOfMaterials.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                .Include(item => item.BillOfMaterial).ThenInclude(item => item.Items).ThenInclude(item => item.Substitutes)
                .Where(item => item.ProductId == batch.ProductId && item.DeletedAt == null && item.IsActive)
                .OrderByDescending(item => item.EffectiveDate).ToListAsync();
            var bom = boms.FirstOrDefault();
            if (bom?.BillOfMaterial is null || bom.BillOfMaterial.DeletedAt.HasValue)
                return Error.Validation("Repair.Bom", "Save a valid active BOM on the product before repairing this batch.");
            if (boms.Count(item => item.EffectiveDate == bom.EffectiveDate) != 1)
                return Error.Validation("Repair.Bom", "The current BOM is ambiguous. Correct the product's active BOM first.");
            var items = bom.BillOfMaterial.Items.Where(item => item.DeletedAt == null).OrderBy(item => item.Order).ThenBy(item => item.MaterialId).ToList();
            definitions.AddRange(items.Select(item => new SourceLine(item.MaterialId, item.BaseUoMId, item.BaseQuantity,
                item.PrescribedQuantity, 0, 0, null, item.UseDensityConversion,
                item.Substitutes.Where(sub => sub.DeletedAt == null).Select(sub => sub.SubstituteMaterialId).Order().ToList(),
                SubstituteQuantities(item.Substitutes))));
            source = Json(new { bom.Id, bom.EffectiveDate, bom.UpdatedAt, bom.BillOfMaterial.Version, Definitions = definitions,
                Items = items.Select(item => new { item.Id, item.UpdatedAt, item.Grade, item.CasNumber, item.Order, item.IsSubstitutable }) });
        }
        else
        {
            var packingStyleId = packingStyleOverride ?? batch.ProductPackingId;
            if (!packingStyleId.HasValue || !await context.ProductPackings.IgnoreQueryFilters()
                .AnyAsync(item => item.Id == packingStyleId && item.ProductId == batch.ProductId && item.DeletedAt == null))
                return Error.Validation("Repair.PackingStyle", "The batch's saved packing style is missing or no longer belongs to this product.");
            var packages = await context.ProductPackages.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                .Include(item => item.Substitutes).Where(item => item.ProductId == batch.ProductId && item.DeletedAt == null
                    && (item.ProductPackingId == null || item.ProductPackingId == packingStyleId)).OrderBy(item => item.MaterialId).ToListAsync();
            packages = ProductPackagingSelection.ForStyle(packages, packingStyleId);
            definitions.AddRange(packages.Select(item => new SourceLine(item.MaterialId, null, item.BaseQuantity,
                item.PrescribedQuantity, item.Loose, item.UnitCapacity, item.DirectLinkMaterialId,
                false, item.Substitutes.Where(sub => sub.DeletedAt == null).Select(sub => sub.SubstituteMaterialId).Order().ToList())));
            source = Json(new { PackingStyleId = packingStyleId, Items = packages.Select(item => new { item.Id, item.UpdatedAt, item.ProductPackingId,
                item.PackingExcessMargin, item.MaterialThickness, item.OtherStandards }).ToList(), Definitions = definitions });
        }
        if (definitions.Count == 0 || definitions.GroupBy(item => item.MaterialId).Any(group => group.Count() != 1))
            blockers.Add("The product must have nonempty, unique material requirements for this batch.");
        var ids = definitions.SelectMany(item => item.Substitutes.Append(item.MaterialId)).Distinct().ToList();
        var departments = await context.MaterialDepartments.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Include(item => item.Material).Include(item => item.UoM).Include(item => item.DensityUoM)
            .Where(item => ids.Contains(item.MaterialId) && item.DepartmentId == departmentId && item.DeletedAt == null).ToListAsync();
        var units = await context.UnitOfMeasures.IgnoreQueryFilters().AsNoTracking().Where(item => item.DeletedAt == null).ToListAsync();
        var current = await context.MaterialBatchReservedQuantities.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.ProductionScheduleProductId == batchId && item.ReleasedAt == null && item.DeletedAt == null)
            .Select(item => item.MaterialBatch.MaterialId).Distinct().ToListAsync();
        var requested = await context.RequisitionItems.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.Requisition.ProductionScheduleProductId == batchId && item.Requisition.RequisitionType == RequisitionType.Stock
                && item.Requisition.DeletedAt == null && item.DeletedAt == null && item.Material.Kind == kind)
            .Select(item => item.MaterialId).Distinct().ToListAsync();
        current = current.Union(requested).ToList();
        var requirements = new List<ProductionMaterialRequirement>();
        // One material option in its department stock unit, or the reason it cannot be used.
        (ProductionMaterialOption Option, string Blocker) Resolve(SourceLine definition, Guid materialId, Guid? baseUnitId, decimal baseQuantity, decimal prescribed)
        {
            var links = departments.Where(item => item.MaterialId == materialId).ToList();
            if (links.Count != 1 || links[0].Material.DeletedAt.HasValue || links[0].Material.Kind != kind
                || links[0].UoM is null || links[0].UoM.DeletedAt.HasValue)
                return (null, $"Material {materialId} needs one valid stock unit and department link of the correct material kind.");
            var link = links[0];
            var sourceUnit = kind == MaterialKind.Raw ? units.SingleOrDefault(unit => unit.Id == baseUnitId) : link.UoM;
            if (sourceUnit is null || sourceUnit.DeletedAt.HasValue)
                return (null, $"{link.Material.Name}: choose a valid BOM source unit before starting production.");
            var quantity = prescribed / (batch.BatchSize == BatchSize.Half ? 2 : 1) + definition.Allowance;
            var converted = definition.UseDensityConversion
                ? MaterialDensity.ConvertBomMass(quantity, sourceUnit.Symbol, link)
                : UomConverter.Convert(quantity, sourceUnit.Symbol, link.UoM.Symbol);
            if (prescribed <= 0 || definition.Allowance < 0 || converted is null or <= 0)
                return (null, definition.UseDensityConversion
                    ? $"{link.Material.Name}: verify the BOM quantity, measured density with an active g/mL unit, and liquid stock unit."
                    : $"{link.Material.Name}: correct the quantity and compatible units on the product first.");
            return (new ProductionMaterialOption
            {
                MaterialId = materialId, Name = link.Material.Name, UnitId = link.UoMId, Unit = link.UoM.Symbol, Quantity = converted.Value,
                BaseQuantity = baseQuantity, BaseUnitId = sourceUnit.Id, BaseUnit = sourceUnit.Symbol, PrescribedQuantity = prescribed,
                DensityGramsPerMilliliter = definition.UseDensityConversion ? link.Density : 0
            }, null);
        }

        foreach (var definition in definitions)
        {
            var choices = current.Where(id => id == definition.MaterialId || definition.Substitutes.Contains(id)).ToList();
            if (choices.Count > 1) { blockers.Add("More than one main/substitute allocation exists for a material. Reconcile it before repair."); continue; }
            var selected = choices.Count == 1 ? choices[0]
                : preferred is not null && preferred.TryGetValue(definition.MaterialId, out var wanted) && definition.Substitutes.Contains(wanted) ? wanted
                : definition.MaterialId;
            var main = Resolve(definition, definition.MaterialId, definition.BaseUnitId, definition.BaseQuantity, definition.Prescribed);
            if (main.Option is null) { blockers.Add(main.Blocker); continue; }
            // A substitute carries its own quantity: it may differ in strength or water content from the main material.
            var substitutes = (definition.SubstituteQuantities ?? []).Where(item => definition.Substitutes.Contains(item.MaterialId))
                .Select(item => (item.MaterialId, Resolved: Resolve(definition, item.MaterialId, item.BaseUnitId, item.BaseQuantity, item.Prescribed))).ToList();
            ProductionMaterialOption chosen = main.Option;
            if (selected != definition.MaterialId)
            {
                var substitute = substitutes.SingleOrDefault(item => item.MaterialId == selected);
                if (substitute.Resolved.Option is null)
                {
                    var name = departments.FirstOrDefault(item => item.MaterialId == selected)?.Material?.Name ?? selected.ToString();
                    blockers.Add(substitute.Resolved.Blocker ?? $"{name}: this substitute is allocated to the batch but has no quantity of its own on the BOM. Set the substitute's quantity on the product BOM first.");
                    continue;
                }
                chosen = substitute.Resolved.Option;
            }
            var options = substitutes.Where(item => item.Resolved.Option is not null).Select(item => item.Resolved.Option).ToList();
            requirements.Add(Requirement(new ProductionMaterialRequirement
            {
                SourceMaterialId = definition.MaterialId, Allowance = definition.Allowance, UnitCapacity = definition.Capacity,
                DirectLinkMaterialId = definition.DirectLink, UseDensityConversion = definition.UseDensityConversion,
                Options = options.Count == 0 ? null : options.Prepend(main.Option).ToList()
            }, chosen));
        }
        if (requirements.GroupBy(item => item.MaterialId).Any(group => group.Count() > 1))
            blockers.Add("A material is selected for more than one requirement. Correct the overlapping substitutes first.");
        var hash = Hash(new { source, batch.ProductId, batch.BatchSize, batch.Quantity, PackingStyleId = kind == MaterialKind.Package ? packingStyleOverride ?? batch.ProductPackingId : null,
            batch.Product.UpdatedAt, DepartmentId = departmentId, StorageId = storage[0].Id, ProductionId = production[0].Id, Requirements = requirements });
        return new ProductionMaterialSource(batch, departmentId.Value, storage[0].Id, production[0].Id, requirements, hash, source, blockers);
    }

    /// <summary>The requirement issued as <paramref name="option"/>; every other field, including the options, is kept.</summary>
    internal static ProductionMaterialRequirement Requirement(ProductionMaterialRequirement requirement, ProductionMaterialOption option) => requirement with
    {
        MaterialId = option.MaterialId, Name = option.Name, UnitId = option.UnitId, Unit = option.Unit, Quantity = option.Quantity,
        BaseQuantity = option.BaseQuantity, PrescribedQuantity = option.PrescribedQuantity, BaseUnitId = option.BaseUnitId,
        BaseUnit = option.BaseUnit, DensityGramsPerMilliliter = option.DensityGramsPerMilliliter
    };

    /// <summary>Substitutes saved with their own quantity; null when none has one, so older definitions hash as before.</summary>
    private static List<SubstituteLine> SubstituteQuantities(IEnumerable<DOMAIN.Entities.BillOfMaterials.BillOfMaterialItemSubstitute> substitutes)
    {
        var lines = substitutes.Where(sub => sub.DeletedAt == null && sub.PrescribedQuantity.HasValue && sub.BaseQuantity.HasValue)
            .OrderBy(sub => sub.SubstituteMaterialId)
            .Select(sub => new SubstituteLine(sub.SubstituteMaterialId, sub.BaseUoMId, sub.BaseQuantity.Value, sub.PrescribedQuantity.Value)).ToList();
        return lines.Count == 0 ? null : lines;
    }

    private sealed record SourceLine(Guid MaterialId, Guid? BaseUnitId, decimal BaseQuantity, decimal Prescribed,
        decimal Allowance, decimal Capacity, Guid? DirectLink, bool UseDensityConversion, List<Guid> Substitutes,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] List<SubstituteLine> SubstituteQuantities = null);

    private sealed record SubstituteLine(Guid MaterialId, Guid? BaseUnitId, decimal BaseQuantity, decimal Prescribed);
}
