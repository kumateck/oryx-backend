using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Users;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class MaterialRepository
{
    public async Task<Result> CreateMaterialDepartment(List<CreateMaterialDepartment> materialDepartments, Guid userId)
    {
        if (context.Database.IsRelational() && context.Database.CurrentTransaction is null)
            return await ProductionMaterialTransaction.Run(context, () => CreateMaterialDepartment(materialDepartments, userId));
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDisabled);
        if (user is null) return UserErrors.NotFound(userId);
        if (!user.DepartmentId.HasValue) return UserErrors.DepartmentNotFound;
        if (materialDepartments.Count == 0 || materialDepartments.Select(m => m.MaterialId).Distinct().Count() != materialDepartments.Count)
            return Error.Validation("MaterialDepartment.Validation", "Choose each material exactly once.");
        var ids = materialDepartments.Select(m => m.MaterialId).ToList();
        var materials = await context.Materials.Where(m => ids.Contains(m.Id)).ToDictionaryAsync(m => m.Id);
        if (materials.Count != ids.Count) return Error.Validation("MaterialDepartment.Material", "A material no longer exists.");
        var links = await context.MaterialDepartments.IgnoreQueryFilters().Include(m => m.Material).Include(m => m.DensityUoM)
            .Where(m => ids.Contains(m.MaterialId) && m.DepartmentId == user.DepartmentId && m.DeletedAt == null).ToListAsync();
        if (links.GroupBy(link => link.MaterialId).Any(group => group.Count() != 1))
            return Error.Conflict("MaterialDepartment.Duplicate", "Reconcile duplicate department links first.");
        var units = await context.UnitOfMeasures.Where(u => u.DeletedAt == null).ToDictionaryAsync(u => u.Id);
        foreach (var dto in materialDepartments)
        {
            if (!units.ContainsKey(dto.UoMId)) return Error.Validation("MaterialDepartment.Unit", "Choose an active stock unit.");
            if (dto.DensityUoMId.HasValue && (dto.Density <= 0 || !units.TryGetValue(dto.DensityUoMId.Value, out var unit)
                || !string.Equals(unit.Symbol?.Trim(), "g/mL", StringComparison.OrdinalIgnoreCase)))
                return Error.Validation("MaterialDepartment.Density", "Enter a positive measured density using an active g/mL unit.");
        }
        var grants = await CurrentPermissionSet.Load(context, userId);
        foreach (var dto in materialDepartments)
        {
            var material = materials[dto.MaterialId];
            if (!grants.Contains(MaterialPermissions.Key(MaterialTask.Link, material.Kind)))
                return Error.Forbidden("MaterialDepartment.Permission", "You do not have permission to link this material kind.");
            if (dto.DensityUoMId.HasValue && dto.Density != material.DensityGramsPerMilliliter
                && !grants.Contains(material.Kind == MaterialKind.Raw ? PermissionKeys.CanEditRawMaterialDensity : PermissionKeys.CanEditPackagingMaterialDensity))
                return Error.Forbidden("MaterialDepartment.DensityPermission", "You do not have permission to change this material's shared density.");
        }
        // Levels are stored in the counting unit without a unit of their own. Changing the unit here would silently
        // re-read them (and every applied batch and requisition) in the new unit, so an existing link changes its
        // unit only through the confirmed counting-unit change, which converts them.
        if (materialDepartments.Any(dto => links.Any(link => link.MaterialId == dto.MaterialId && link.UoMId != dto.UoMId)))
            return Error.Conflict("MaterialDepartment.CountingUnit", "To change the unit your department counts a material in, use Change counting unit. It converts the stock levels and every quantity kept in the old unit.");
        // A new link must be able to read every physical lot already in this department in its unit.
        foreach (var dto in materialDepartments)
        {
            var existing = links.SingleOrDefault(link => link.MaterialId == dto.MaterialId);
            if (existing is not null) continue;
            var lotUnits = await context.ShelfMaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes()
                .Where(row => row.DeletedAt == null && row.Quantity != 0 && row.MaterialBatch.DeletedAt == null
                    && row.MaterialBatch.MaterialId == dto.MaterialId
                    && row.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.DepartmentId == user.DepartmentId)
                .Select(row => row.MaterialBatch.UoM.Symbol).Distinct().ToListAsync();
            var density = dto.DensityUoMId.HasValue ? dto.Density : materials[dto.MaterialId].DensityGramsPerMilliliter;
            if (lotUnits.Any(unit => !MaterialDensity.Convert(1, unit, units[dto.UoMId].Symbol, density).HasValue))
                return Error.Conflict("MaterialDepartment.StockUnit", "Existing stock cannot be converted to that unit. For mass and volume, record a positive measured density in g/mL first.");
        }
        grants = await CurrentPermissionSet.Load(context, userId);
        foreach (var dto in materialDepartments)
        {
            var material = materials[dto.MaterialId];
            if (!grants.Contains(MaterialPermissions.Key(MaterialTask.Link, material.Kind)))
                return Error.Forbidden("MaterialDepartment.Permission", "You do not have permission to link this material kind.");
            if (dto.DensityUoMId.HasValue && dto.Density != material.DensityGramsPerMilliliter
                && !grants.Contains(material.Kind == MaterialKind.Raw ? PermissionKeys.CanEditRawMaterialDensity : PermissionKeys.CanEditPackagingMaterialDensity))
                return Error.Forbidden("MaterialDepartment.DensityPermission", "You do not have permission to change this material's shared density.");
        }
        foreach (var dto in materialDepartments)
        {
            var material = materials[dto.MaterialId];
            if (dto.DensityUoMId.HasValue && dto.Density != material.DensityGramsPerMilliliter)
            {
                context.MaterialDensityMeasurements.Add(new() { Id = Guid.NewGuid(), ActorId = userId, MaterialId = material.Id,
                    RecordedAt = DateTime.UtcNow, PreviousDensity = material.DensityGramsPerMilliliter, Density = dto.Density });
                material.DensityGramsPerMilliliter = dto.Density;
                material.LastUpdatedById = userId;
            }
            var existing = links.SingleOrDefault(m => m.MaterialId == dto.MaterialId);
            if (existing is null)
            {
                existing = new MaterialDepartment { MaterialId = dto.MaterialId, DepartmentId = user.DepartmentId.Value };
                context.MaterialDepartments.Add(existing);
            }
            existing.ReOrderLevel = dto.ReOrderLevel; existing.MinimumStockLevel = dto.MinimumStockLevel;
            existing.MaximumStockLevel = dto.MaximumStockLevel; existing.UoMId = dto.UoMId;
            existing.LastUpdatedById = userId;
            // Keep legacy measurements intact unless the caller supplies a verified replacement.
            if (dto.DensityUoMId.HasValue) { existing.Density = dto.Density; existing.DensityUoMId = dto.DensityUoMId; }
            else if (!existing.DensityUoMId.HasValue && dto.Density != 0) existing.Density = dto.Density;
        }
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
