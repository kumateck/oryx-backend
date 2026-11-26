using System.Text.Json;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>
/// A department changes the unit it counts a material in. Everything the department stores in its counting unit is
/// re-expressed in the new one: the stock levels, every applied production configuration line and every requisition
/// item. Each value is converted exactly (mass and volume through the material's verified density), so the physical
/// amount is unchanged. Lots, shelf rows, bin-card rows and holds already record their own unit and are read through
/// conversion, so they are not written: the preview shows the department's stock in both units to prove it.
/// </summary>
public partial class MaterialRepository
{
    private static string CountingUnitPermission(MaterialKind kind) =>
        kind == MaterialKind.Raw ? PermissionKeys.CanChangeRawMaterialCountingUnit : PermissionKeys.CanChangePackagingMaterialCountingUnit;

    public async Task<Result<MaterialCountingUnitPreview>> PreviewCountingUnitChange(Guid materialId, MaterialKind kind, Guid unitId, Guid actor)
    {
        var plan = await PlanCountingUnitChange(materialId, kind, unitId, actor);
        return plan.IsSuccess ? plan.Value.Preview : plan.Error;
    }

    public async Task<Result<MaterialCountingUnitResult>> ChangeCountingUnit(Guid materialId, MaterialKind kind, ChangeMaterialCountingUnit request, Guid actor)
    {
        if (context.Database.IsRelational() && context.Database.CurrentTransaction is null)
            return await ProductionMaterialTransaction.Run(context, () => ChangeCountingUnit(materialId, kind, request, actor));
        var reason = request?.Reason?.Trim();
        if (reason is null || reason.Length is < 5 or > 2000)
            return Error.Validation("CountingUnit.Reason", "Give a reason between five and 2,000 characters.");
        var planned = await PlanCountingUnitChange(materialId, kind, request.UnitId, actor, track: true);
        if (planned.IsFailure) return planned.Error;
        var plan = planned.Value;
        if (plan.Preview.Blockers.Count > 0)
            return Error.Conflict("CountingUnit.Blocked", string.Join(" ", plan.Preview.Blockers));
        if (!string.Equals(plan.Preview.PreviewHash, request.PreviewHash, StringComparison.Ordinal))
            return Error.Conflict("CountingUnit.Stale", "Something this change converts has changed since the preview. Refresh the preview and review it again.");
        if (!plan.Preview.HasChanges) return new MaterialCountingUnitResult { Changed = false };
        if (!(await CurrentPermissionSet.Load(context, actor)).Contains(CountingUnitPermission(kind)))
            return Error.Forbidden("CountingUnit.Permission", "You do not have permission to change this material's counting unit.");

        var now = DateTime.UtcNow;
        var link = plan.Link;
        var target = plan.Target;
        if (link.UoMId != target.Id)
        {
            link.MinimumStockLevel = plan.Preview.Levels[0].After;
            link.ReOrderLevel = plan.Preview.Levels[1].After;
            link.MaximumStockLevel = plan.Preview.Levels[2].After;
            link.UoMId = target.Id;
            link.LastUpdatedById = actor;
        }
        foreach (var (configuration, lines) in plan.Configurations)
        {
            configuration.RequirementsJson = ProductionMaterialSources.Json(lines);
            configuration.Revision++;
            configuration.LastUpdatedById = actor;
        }
        foreach (var (item, quantity, received, issued) in plan.Items)
        {
            item.Quantity = quantity;
            item.QuantityReceived = received;
            item.IssuedQuantity = issued;
            item.UoMId = target.Id;
            item.LastUpdatedById = actor;
        }
        foreach (var (extra, quantity) in plan.Extras)
        {
            extra.Quantity = quantity;
            extra.UoMId = target.Id;
            extra.LastUpdatedById = actor;
        }
        foreach (var write in plan.PackagingWrites)
            write();
        var change = new MaterialCountingUnitChange
        {
            Id = Guid.NewGuid(), MaterialId = materialId, DepartmentId = link.DepartmentId, MaterialDepartmentId = link.Id,
            ActorId = actor, RecordedAt = now, FromUnitId = plan.Preview.FromUnitId, ToUnitId = target.Id,
            DensityGramsPerMilliliter = plan.Preview.DensityGramsPerMilliliter, PreviewHash = plan.Preview.PreviewHash,
            Reason = reason, PreviewJson = JsonSerializer.Serialize(plan.Preview)
        };
        context.MaterialCountingUnitChanges.Add(change);
        if (!(await CurrentPermissionSet.Load(context, actor)).Contains(CountingUnitPermission(kind)))
            return Error.Forbidden("CountingUnit.Permission", "You do not have permission to change this material's counting unit.");
        await context.SaveChangesAsync();
        return new MaterialCountingUnitResult { ChangeId = change.Id, Changed = true };
    }

    private sealed record CountingUnitPlan(
        MaterialCountingUnitPreview Preview, MaterialDepartment Link, UnitOfMeasure Target,
        List<(ProductionMaterialConfiguration Configuration, List<ProductionMaterialRequirement> Lines)> Configurations,
        List<(RequisitionItem Item, decimal Quantity, decimal Received, decimal? Issued)> Items,
        List<(ProductionExtraPacking Request, decimal Quantity)> Extras, List<Action> PackagingWrites);

    private async Task<Result<CountingUnitPlan>> PlanCountingUnitChange(Guid materialId, MaterialKind kind, Guid unitId, Guid actor, bool track = false)
    {
        if (!Enum.IsDefined(kind)) return Error.Validation("CountingUnit.Kind", "Choose raw or packaging.");
        var user = await context.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.Id == actor && !item.IsDisabled && item.DeletedAt == null)
            .Select(item => new { item.DepartmentId }).SingleOrDefaultAsync();
        if (user is null) return Error.Forbidden("CountingUnit.Actor", "Your account is not active.");
        if (!user.DepartmentId.HasValue) return UserErrors.DepartmentNotFound;
        if (!(await CurrentPermissionSet.Load(context, actor)).Contains(CountingUnitPermission(kind)))
            return Error.Forbidden("CountingUnit.Permission", "You do not have permission to change this material's counting unit.");
        var departmentId = user.DepartmentId.Value;
        var material = await context.Materials.IgnoreQueryFilters().AsNoTracking().IgnoreAutoIncludes()
            .SingleOrDefaultAsync(item => item.Id == materialId && item.DeletedAt == null);
        if (material is null) return Error.NotFound("CountingUnit.Material", "The material no longer exists.");
        if (material.Kind != kind) return Error.Forbidden("CountingUnit.Kind", "This action does not match the material kind.");
        var linkQuery = context.MaterialDepartments.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Include(item => item.Material).Include(item => item.UoM).Include(item => item.DensityUoM)
            .Where(item => item.MaterialId == materialId && item.DepartmentId == departmentId && item.DeletedAt == null);
        var links = await (track ? linkQuery : linkQuery.AsNoTracking()).ToListAsync();
        if (links.Count != 1 || links[0].UoM is null)
            return Error.Conflict("CountingUnit.Link", "Your department must keep this material, with one counting unit, before the unit can change.");
        var link = links[0];
        var target = await context.UnitOfMeasures.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(unit => unit.Id == unitId && unit.DeletedAt == null);
        if (target is null) return Error.Validation("CountingUnit.Unit", "Choose an active unit.");

        var blockers = new List<string>();
        var from = link.UoM;
        var crosses = UomConverter.Convert(1m, from.Symbol, target.Symbol) is null;
        var density = MaterialDensity.Verified(link);
        var densityUsed = crosses ? density : null;
        if (crosses && (density is null || MaterialDensity.Convert(1m, from.Symbol, target.Symbol, density) is null))
            blockers.Add(density is null && MaterialDensity.Convert(1m, from.Symbol, target.Symbol, 1m) is not null
                ? $"{material.Name}: {from.Symbol} and {target.Symbol} differ in mass and volume. Record a verified density in g/mL first."
                : $"{material.Name}: {from.Symbol} cannot be converted to {target.Symbol}.");
        CountingUnitValue Value(string label, decimal before, string unit, string afterUnit)
        {
            var direct = MaterialDensity.Convert(before, unit, afterUnit, null);
            var after = direct ?? MaterialDensity.Convert(before, unit, afterUnit, density);
            if (direct is null && after is not null) densityUsed = density;
            if (after is null || (before != 0 && after == 0))
                blockers.Add($"{label}: {before} {unit} cannot be converted to {afterUnit}. Verify the units and, for mass and volume, the density in g/mL.");
            return new CountingUnitValue { Label = label, Before = before, BeforeUnit = unit, After = after ?? 0, AfterUnit = afterUnit };
        }

        // The stock-level contract stores mass in mg and volume in mL for all links in those families.
        var fromLevelUnit = MaterialDensity.StockLevelUnit(from.Symbol);
        var toLevelUnit = MaterialDensity.StockLevelUnit(target.Symbol);

        var levels = new List<CountingUnitValue>
        {
            Value("Minimum level", link.MinimumStockLevel, fromLevelUnit, toLevelUnit),
            Value("Re-order level", link.ReOrderLevel, fromLevelUnit, toLevelUnit),
            Value("Maximum level", link.MaximumStockLevel, fromLevelUnit, toLevelUnit),
        };

        // Physical stock and holds keep their own unit. Read here only to show the same amount in both units.
        var storeType = kind == MaterialKind.Raw ? WarehouseType.RawMaterialStorage : WarehouseType.PackagedStorage;
        var shelf = await context.ShelfMaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(row => row.DeletedAt == null && row.Quantity != 0 && row.MaterialBatch.DeletedAt == null && row.MaterialBatch.MaterialId == materialId
                && row.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.DepartmentId == departmentId
                && row.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Type == storeType)
            .OrderBy(row => row.Id).Select(row => new { row.Quantity, Unit = row.MaterialBatch.UoM.Symbol }).ToListAsync();
        var holds = await ProductionMaterialAllocator.UnissuedReservations(context).IgnoreAutoIncludes().AsNoTracking()
            .Where(row => row.DeletedAt == null && row.ReleasedAt == null && row.MaterialBatch.MaterialId == materialId
                && (row.WarehouseLocationShelfId.HasValue
                    ? row.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.DepartmentId == departmentId
                        && row.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Type == storeType
                    : row.Warehouse.DepartmentId == departmentId))
            .OrderBy(row => row.Id).Select(row => new { row.Quantity, Unit = row.UoM.Symbol, row.DensityGramsPerMilliliter }).ToListAsync();
        CountingUnitValue Total(string label, IEnumerable<(decimal Quantity, string Unit, decimal? Density)> rows)
        {
            decimal before = 0, after = 0;
            foreach (var row in rows)
            {
                var inFrom = MaterialDensity.Convert(row.Quantity, row.Unit, from.Symbol, row.Density is > 0 ? row.Density : density);
                var inTarget = MaterialDensity.Convert(row.Quantity, row.Unit, target.Symbol, row.Density is > 0 ? row.Density : density);
                if (inFrom is null || inTarget is null) { blockers.Add($"{label}: {row.Quantity} {row.Unit} cannot be read in {from.Symbol} and {target.Symbol}."); continue; }
                before += inFrom.Value; after += inTarget.Value;
            }
            return new CountingUnitValue { Label = label, Before = before, BeforeUnit = from.Symbol, After = after, AfterUnit = target.Symbol };
        }
        var stock = Total("Stock", shelf.Select(row => (row.Quantity, row.Unit, (decimal?)null)));
        var reserved = Total("Reserved", holds.Select(row => (row.Quantity, row.Unit, row.DensityGramsPerMilliliter)));

        // Applied production configurations of this department: every line (and substitute option) of this material.
        var configurationQuery = context.ProductionMaterialConfigurations.IgnoreQueryFilters()
            .Where(item => item.DepartmentId == departmentId && item.DeletedAt == null && item.Kind == kind && item.RequirementsJson.Contains(materialId.ToString()));
        var configurations = await (track ? configurationQuery : configurationQuery.AsNoTracking()).OrderBy(item => item.Id).ToListAsync();
        var batchIds = configurations.Select(item => item.ProductionScheduleProductId).ToList();
        var batchNumbers = await context.ProductionScheduleProducts.IgnoreQueryFilters().AsNoTracking()
            .Where(item => batchIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, item => item.BatchNumber);
        var batchRecords = new List<CountingUnitRecord>();
        var configurationWrites = new List<(ProductionMaterialConfiguration, List<ProductionMaterialRequirement>)>();
        foreach (var configuration in configurations)
        {
            var reference = batchNumbers.GetValueOrDefault(configuration.ProductionScheduleProductId) ?? configuration.ProductionScheduleProductId.ToString();
            var values = new List<CountingUnitValue>();
            ProductionMaterialOption ConvertOption(ProductionMaterialOption option)
            {
                if (option.MaterialId != materialId || option.UnitId == target.Id) return option;
                var value = Value(option.Name, option.Quantity, option.Unit, target.Symbol);
                values.Add(value with { Label = $"{option.Name} (option)" });
                return option with { UnitId = target.Id, Unit = target.Symbol, Quantity = value.After,
                    DensityGramsPerMilliliter = density ?? option.DensityGramsPerMilliliter };
            }
            var lines = ProductionMaterialSources.Requirements(configuration.RequirementsJson).Select(line =>
            {
                var options = line.Options?.Select(ConvertOption).ToList();
                if (line.MaterialId != materialId || line.UnitId == target.Id) return line with { Options = options };
                var value = Value(line.Name, line.Quantity, line.Unit, target.Symbol);
                values.Add(value);
                return line with { UnitId = target.Id, Unit = target.Symbol, Quantity = value.After, Options = options,
                    DensityGramsPerMilliliter = density ?? line.DensityGramsPerMilliliter };
            }).ToList();
            if (values.Count == 0) continue;
            batchRecords.Add(new CountingUnitRecord { Id = configuration.Id, Reference = reference, Values = values });
            configurationWrites.Add((configuration, lines));
        }

        // Requisition items of this department. An item without a unit was entered in the counting unit.
        var itemQuery = context.RequisitionItems.IgnoreQueryFilters().Include(item => item.UoM).Include(item => item.Requisition)
            .Where(item => item.MaterialId == materialId && item.DeletedAt == null && item.RetiredByRepairId == null
                && item.Requisition.DepartmentId == departmentId && item.Requisition.DeletedAt == null
                && item.Requisition.RequisitionType != RequisitionType.Purchase
                && (item.UoMId == null || item.UoMId != target.Id));
        var items = await (track ? itemQuery : itemQuery.AsNoTracking()).OrderBy(item => item.Requisition.Code).ThenBy(item => item.Id).ToListAsync();
        var requisitionRecords = new List<CountingUnitRecord>();
        var itemWrites = new List<(RequisitionItem, decimal, decimal, decimal?)>();
        foreach (var item in items)
        {
            var unit = item.UoM is { DeletedAt: null } own ? own.Symbol : item.UoMId.HasValue ? null : from.Symbol;
            if (unit is null) { blockers.Add($"{item.Requisition.Code}: the item's unit no longer exists."); continue; }
            var values = new List<CountingUnitValue> { Value("Requested", item.Quantity, unit, target.Symbol) };
            if (item.QuantityReceived != 0) values.Add(Value("Received", item.QuantityReceived, unit, target.Symbol));
            if (item.IssuedQuantity is decimal issued) values.Add(Value("Issued", issued, unit, target.Symbol));
            requisitionRecords.Add(new CountingUnitRecord { Id = item.Id, Reference = item.Requisition.Code, Values = values });
            itemWrites.Add((item, values[0].After, item.QuantityReceived == 0 ? 0 : values[1].After,
                item.IssuedQuantity.HasValue ? values[^1].After : null));
        }

        var extraQuery = context.ProductionExtraPackings.IgnoreQueryFilters().Include(item => item.UoM)
            .Include(item => item.ProductionScheduleProduct)
            .Where(item => item.MaterialId == materialId && item.DeletedAt == null && item.UoMId != target.Id
                && item.ProductionScheduleProduct.Product.DepartmentId == departmentId
                && item.ProductionScheduleProduct.Product.DeletedAt == null
                && item.ProductionScheduleProduct.ProductionSchedule.DeletedAt == null);
        var extras = await (track ? extraQuery : extraQuery.AsNoTracking()).OrderBy(item => item.Id).ToListAsync();
        var extraRecords = new List<CountingUnitRecord>();
        var extraWrites = new List<(ProductionExtraPacking, decimal)>();
        foreach (var extra in extras)
        {
            var reference = extra.ProductionScheduleProduct.BatchNumber ?? extra.ProductionScheduleProductId.ToString();
            var value = Value("Extra requested", extra.Quantity, extra.UoM?.Symbol, target.Symbol);
            extraRecords.Add(new CountingUnitRecord { Id = extra.Id, Reference = reference, Values = [value] });
            extraWrites.Add((extra, value.After));
        }

        var (packagingRecords, packagingWrites) = kind == MaterialKind.Package && from.Id != target.Id
            ? await PlanCountingUnitPackaging(materialId, departmentId, from.Symbol, target.Symbol, actor, Value, track)
            : ([], []);
        var preview = new MaterialCountingUnitPreview
        {
            MaterialId = materialId, Material = material.Name, Code = material.Code, DepartmentId = departmentId,
            FromUnitId = from.Id, FromUnit = from.Symbol, ToUnitId = target.Id, ToUnit = target.Symbol,
            DensityGramsPerMilliliter = densityUsed,
            HasChanges = from.Id != target.Id || batchRecords.Count > 0 || requisitionRecords.Count > 0 || extraRecords.Count > 0,
            Levels = levels, Stock = stock, Reserved = reserved, Batches = batchRecords, Requisitions = requisitionRecords,
            ExtraRequests = extraRecords, Packaging = packagingRecords,
            Blockers = blockers.Distinct().ToList()
        };
        preview = preview with { PreviewHash = ProductionMaterialSources.Hash(new { preview, link.Id, link.UpdatedAt,
            Configurations = configurations.Select(item => new { item.Id, item.Revision }),
            Items = items.Select(item => new { item.Id, item.UpdatedAt }),
            Extras = extras.Select(item => new { item.Id, item.UpdatedAt }) }) };
        return new CountingUnitPlan(preview, link, target, configurationWrites, itemWrites, extraWrites, packagingWrites);
    }
}
