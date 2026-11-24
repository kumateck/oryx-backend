using System.Text.Json;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Materials;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class MaterialRepository
{
    public async Task<Result> ClearDensity(Guid materialId, MaterialKind expectedKind, Guid actor)
    {
        if (context.Database.IsRelational() && context.Database.CurrentTransaction is null)
            return await ProductionMaterialTransaction.Run(context, () => ClearDensity(materialId, expectedKind, actor));
        if (!await context.Users.AnyAsync(user => user.Id == actor && !user.IsDisabled && user.DeletedAt == null))
            return Error.Forbidden("MaterialDensity.Actor", "Your account is not active.");
        var material = await context.Materials.IgnoreAutoIncludes().SingleOrDefaultAsync(item => item.Id == materialId && item.DeletedAt == null);
        if (material is null) return Error.NotFound("MaterialDensity.NotFound", "The material no longer exists.");
        if (material.Kind != expectedKind) return Error.Forbidden("MaterialDensity.Kind", "This action does not match the material kind.");
        var permission = material.Kind == MaterialKind.Raw ? PermissionKeys.CanEditRawMaterialDensity : PermissionKeys.CanEditPackagingMaterialDensity;
        if (!(await CurrentPermissionSet.Load(context, actor)).Contains(permission))
            return Error.Forbidden("MaterialDensity.Permission", "You do not have permission to change this material's shared density.");
        // Clear every legacy fallback, including archived links that could later be restored.
        // Physical quantities, stock units and the density captured on existing holds remain intact.
        var links = await context.MaterialDepartments.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Where(link => link.MaterialId == materialId).OrderBy(link => link.Id).ToListAsync();
        if (!(await CurrentPermissionSet.Load(context, actor)).Contains(permission))
            return Error.Forbidden("MaterialDensity.Permission", "You do not have permission to change this material's shared density.");
        if (material.DensityGramsPerMilliliter is null && links.All(link => link.Density == 0 && link.DensityUoMId == null))
            return Result.Success();
        context.MaterialDensityMeasurements.Add(new() {
            Id = Guid.NewGuid(), MaterialId = material.Id, ActorId = actor, RecordedAt = DateTime.UtcNow,
            PreviousDensity = material.DensityGramsPerMilliliter, Density = null,
            PreviousDepartmentDensitiesJson = JsonSerializer.Serialize(links.Select(link => new {
                link.Id, link.DepartmentId, link.Density, link.DensityUoMId, link.DeletedAt
            }))
        });
        material.DensityGramsPerMilliliter = null;
        material.LastUpdatedById = actor;
        foreach (var link in links)
        {
            link.Density = 0;
            link.DensityUoMId = null;
            link.LastUpdatedById = actor;
        }
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
