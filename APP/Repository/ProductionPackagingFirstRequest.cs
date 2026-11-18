using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
namespace APP.Repository;

/// <summary>Adopt newly configured packaging once, on its first stock request, when none was adopted on Start.</summary>
internal static class ProductionPackagingFirstRequest
{
    internal static async Task<Result> Prepare(ApplicationDbContext db, Guid batchId, Guid stepId, Guid actorId, List<CreateRequisitionItemRequest> requested)
    {
        if (requested.Count == 0 || await db.ProductionMaterialConfigurations.AnyAsync(c => c.ProductionScheduleProductId == batchId && c.Kind == MaterialKind.Package))
            return Result.Success();
        var loaded = await ProductionMaterialSources.Load(db, batchId, MaterialKind.Package);
        if (loaded.IsFailure) return loaded.Error;
        var source = loaded.Value;
        if (!await db.Users.AnyAsync(u => u.Id == actorId && !u.IsDisabled && u.DepartmentId == source.DepartmentId)
            || !(await CurrentPermissionSet.Load(db, actorId)).Contains(PermissionKeys.CanUpdateProductionStep)
            || !await db.ProductionActivitySteps.AnyAsync(s => s.Id == stepId && s.ProductionActivity.ProductionScheduleProductId == batchId && s.Status != DOMAIN.Entities.Base.ProductionStatus.Completed && s.Status != DOMAIN.Entities.Base.ProductionStatus.Cancelled))
            return Error.Forbidden("PackagingRequest.Permission", "Request packaging from an active step of a batch in your production department.");
        if (source.Blockers.Count != 0 || requested.Count != source.Requirements.Count || source.Requirements.Any(line =>
            !requested.Any(item => item.MaterialId == line.MaterialId && item.UomId == line.UnitId && item.Quantity == line.Quantity)))
            return Error.Conflict("PackagingRequest.Configuration", "Reload the batch and request its newly saved packaging materials and quantities.");
        if (await db.MaterialBatchReservedQuantities.IgnoreQueryFilters().AnyAsync(r => r.ProductionScheduleProductId == batchId && r.MaterialBatch.Material.Kind == MaterialKind.Package && r.ReleasedAt == null))
            return Error.Conflict("PackagingRequest.Reservations", "Existing packaging allocations need reconciliation before the first request.");
        var allocation = await ProductionMaterialAllocator.Plan(db, source, []);
        if (allocation.Blockers.Count != 0 || allocation.Shortages.Values.Any(q => q > 0))
            return Error.Conflict("PackagingRequest.Stock", "There is not enough unreserved, usable stock for the packaging request. Cover its shortages and retry.");
        if (!(await CurrentPermissionSet.Load(db, actorId)).Contains(PermissionKeys.CanUpdateProductionStep))
            return Error.Forbidden("PackagingRequest.Permission", "Your current permissions do not allow this stock request.");
        ProductionMaterialAllocator.Apply(db, source, [], allocation.Allocations);
        db.ProductionMaterialConfigurations.Add(new ProductionMaterialConfiguration {
            Id = Guid.NewGuid(), ProductionScheduleProductId = batchId, Kind = MaterialKind.Package, Revision = 1,
            DepartmentId = source.DepartmentId, StorageWarehouseId = source.StorageId, ProductionWarehouseId = source.ProductionId,
            SourceHash = source.SourceHash, RequirementsJson = ProductionMaterialSources.Json(source.Requirements) });
        return Result.Success();
    }
}
