using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

    internal static async Task<Result<ProductionMaterialSource>> Load(ApplicationDbContext context, Guid batchId, MaterialKind kind)
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
                item.PrescribedQuantity, 0, 0, null, item.Substitutes.Where(sub => sub.DeletedAt == null).Select(sub => sub.SubstituteMaterialId).Order().ToList())));
            source = Json(new { bom.Id, bom.EffectiveDate, bom.UpdatedAt, bom.BillOfMaterial.Version, Definitions = definitions,
                Items = items.Select(item => new { item.Id, item.UpdatedAt, item.Grade, item.CasNumber, item.Order, item.IsSubstitutable }) });
        }
        else
        {
            if (!batch.ProductPackingId.HasValue || !await context.ProductPackings.IgnoreQueryFilters()
                .AnyAsync(item => item.Id == batch.ProductPackingId && item.ProductId == batch.ProductId && item.DeletedAt == null))
                return Error.Validation("Repair.PackingStyle", "The batch's saved packing style is missing or no longer belongs to this product.");
            var packages = await context.ProductPackages.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                .Include(item => item.Substitutes).Where(item => item.ProductId == batch.ProductId && item.DeletedAt == null
                    && (item.ProductPackingId == null || item.ProductPackingId == batch.ProductPackingId)).OrderBy(item => item.MaterialId).ToListAsync();
            definitions.AddRange(packages.Select(item => new SourceLine(item.MaterialId, null, item.BaseQuantity,
                item.PrescribedQuantity, item.Loose, item.UnitCapacity, item.DirectLinkMaterialId,
                item.Substitutes.Where(sub => sub.DeletedAt == null).Select(sub => sub.SubstituteMaterialId).Order().ToList())));
            source = Json(new { Items = packages.Select(item => new { item.Id, item.UpdatedAt, item.ProductPackingId,
                item.PackingExcessMargin, item.MaterialThickness, item.OtherStandards }).ToList(), Definitions = definitions });
        }
        if (definitions.Count == 0 || definitions.GroupBy(item => item.MaterialId).Any(group => group.Count() != 1))
            blockers.Add("The product must have nonempty, unique material requirements for this batch.");
        var ids = definitions.SelectMany(item => item.Substitutes.Append(item.MaterialId)).Distinct().ToList();
        var departments = await context.MaterialDepartments.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Include(item => item.Material).Include(item => item.UoM)
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
        foreach (var definition in definitions)
        {
            var choices = current.Where(id => id == definition.MaterialId || definition.Substitutes.Contains(id)).ToList();
            if (choices.Count > 1) { blockers.Add("More than one main/substitute allocation exists for a material. Reconcile it before repair."); continue; }
            var selected = choices.Count == 1 ? choices[0] : definition.MaterialId;
            var links = departments.Where(item => item.MaterialId == selected).ToList();
            if (links.Count != 1 || links[0].Material.DeletedAt.HasValue || links[0].Material.Kind != kind
                || links[0].UoM is null || links[0].UoM.DeletedAt.HasValue)
            { blockers.Add($"Material {selected} needs one valid stock unit and department link of the correct material kind."); continue; }
            var link = links[0];
            var sourceUnit = kind == MaterialKind.Raw ? units.SingleOrDefault(unit => unit.Id == definition.BaseUnitId) : link.UoM;
            var quantity = definition.Prescribed / (batch.BatchSize == BatchSize.Half ? 2 : 1) + definition.Allowance;
            var converted = UomConverter.Convert(quantity, sourceUnit?.Symbol, link.UoM.Symbol);
            if (definition.Prescribed <= 0 || definition.Allowance < 0 || converted is null or <= 0)
            { blockers.Add($"{link.Material.Name}: correct the quantity and compatible units on the product first."); continue; }
            requirements.Add(new ProductionMaterialRequirement
            {
                SourceMaterialId = definition.MaterialId, MaterialId = selected, Name = link.Material.Name,
                UnitId = link.UoMId, Unit = link.UoM.Symbol, Quantity = converted.Value,
                BaseQuantity = definition.BaseQuantity, PrescribedQuantity = definition.Prescribed,
                BaseUnitId = sourceUnit.Id, BaseUnit = sourceUnit.Symbol,
                Allowance = definition.Allowance, UnitCapacity = definition.Capacity, DirectLinkMaterialId = definition.DirectLink
            });
        }
        if (requirements.GroupBy(item => item.MaterialId).Any(group => group.Count() > 1))
            blockers.Add("A material is selected for more than one requirement. Correct the overlapping substitutes first.");
        var hash = Hash(new { source, batch.ProductId, batch.BatchSize, batch.Quantity, batch.ProductPackingId,
            batch.Product.UpdatedAt, DepartmentId = departmentId, StorageId = storage[0].Id, ProductionId = production[0].Id, Requirements = requirements });
        return new ProductionMaterialSource(batch, departmentId.Value, storage[0].Id, production[0].Id, requirements, hash, source, blockers);
    }

    private sealed record SourceLine(Guid MaterialId, Guid? BaseUnitId, decimal BaseQuantity, decimal Prescribed,
        decimal Allowance, decimal Capacity, Guid? DirectLink, List<Guid> Substitutes);
}
