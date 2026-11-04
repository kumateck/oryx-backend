using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;

namespace APP.Services.Production;

public sealed partial class IssuedMaterialRepairService(ApplicationDbContext db, ICurrentUserService identity)
{
    private sealed record Allocation(ShelfMaterialBatch Shelf, MaterialBatchReservedQuantity Reservation, decimal Adjustment);
    private sealed record Plan(ProductionScheduleProduct Batch, Requisition Requisition, Guid SourceWarehouseId,
        Guid ProductionWarehouseId, List<MaterialBatchReservedQuantity> Reservations,
        IssuedMaterialRepairPreviewDto Preview, List<Allocation> Allocations);

    public static string Permission(IssuedMaterialRepairKind kind) => kind == IssuedMaterialRepairKind.Bom
        ? PermissionKeys.CanRepairIssuedBom : PermissionKeys.CanRepairIssuedPackaging;

    private async Task<Result<Guid>> Access(Guid batchId, IssuedMaterialRepairKind kind)
    {
        if (!Enum.IsDefined(kind)) return Error.Validation("Repair.Kind", "Choose BOM or packaging repair.");
        var actor = await db.Users.IgnoreAutoIncludes().Where(user => user.Id == identity.UserId && !user.IsDisabled)
            .Select(user => new { user.DepartmentId, Type = (DepartmentType?)user.Department.Type }).SingleOrDefaultAsync();
        var grants = await CurrentPermissionSet.Load(db, identity.UserId);
        if (actor?.DepartmentId is not { } department || actor.Type != DepartmentType.Production
            || !grants.Contains(PermissionKeys.CanViewPlannedProducts) || !grants.Contains(Permission(kind)))
            return Error.Forbidden("Repair.Permission", "Your current department and permissions do not allow this repair.");
        if (!await db.ProductionScheduleProducts.IgnoreAutoIncludes().AnyAsync(batch => batch.Id == batchId
                && batch.Product.DepartmentId == department && batch.Product.DeletedAt == null))
            return Error.NotFound("Repair.Batch", "Production batch not found in your department.");
        return department;
    }

    public async Task<Result<IssuedMaterialRepairPreviewDto>> Preview(Guid batchId, IssuedMaterialRepairKind kind)
    {
        var access = await Access(batchId, kind);
        if (access.IsFailure) return access.Error;
        // One consistent snapshot; no rows are changed or locked by a preview.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        var plan = await BuildPlan(batchId, kind);
        return plan.IsFailure ? plan.Error : plan.Value.Preview;
    }

    public async Task<Result<IssuedMaterialRepairResultDto>> Apply(Guid batchId, IssuedMaterialRepairKind kind,
        ApplyIssuedMaterialRepairRequest request)
    {
        if (request.RequestId == Guid.Empty || request.PreviewToken.Length != 64 || request.Reason.Trim().Length is < 3 or > 1000)
            return Error.Validation("Repair.Request", "A request ID, current preview and reason (3–1000 characters) are required.");
        var access = await Access(batchId, kind);
        if (access.IsFailure) return access.Error;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '5s'");
        // Repairs are infrequent corrective writes. Locks also conflict with ordinary writers that
        // do not participate in a repair-specific advisory lock (issue, consumption, returns, edits).
        await db.Database.ExecuteSqlRawAsync("""
            LOCK TABLE "Products", "ProductPackages", "ProductBillOfMaterials", "BillOfMaterials", "BillOfMaterialItems",
              "Materials", "MaterialDepartments", "UnitOfMeasures", "Warehouses", "WarehouseLocations", "WarehouseLocationRacks", "WarehouseLocationShelves",
              "ProductionSchedules", "ProductionScheduleProducts", "ProductionActivities", "ProductionActivitySteps", "Operations",
              "Requisitions", "RequisitionItems", "MaterialBatchReservedQuantities", "MaterialBatches", "ShelfMaterialBatches",
              "MassMaterialBatchMovements", "MaterialBatchEvents", "BinCardInformation", "MaterialReturnNotes", "ProductionExtraPackings",
              "FinalPackings", "FinishedGoodsTransferNotes", "IssuedMaterialRepairs", "IssuedMaterialRepairLines", "IssuedMaterialRepairAllocations"
            IN SHARE ROW EXCLUSIVE MODE
            """);
        // Recheck grants under the transaction and again immediately before the single SaveChanges.
        access = await Access(batchId, kind);
        if (access.IsFailure) return access.Error;
        var previous = await db.IssuedMaterialRepairs.AsNoTracking().SingleOrDefaultAsync(repair => repair.Id == request.RequestId);
        if (previous is not null)
        {
            if (previous.ProductionScheduleProductId != batchId || previous.Kind != kind || previous.ActorId != identity.UserId
                || previous.PreviewToken != request.PreviewToken || previous.Reason != request.Reason.Trim())
                return Error.Conflict("Repair.RequestReused", "This request ID belongs to a different repair. Reload the preview.");
            return new IssuedMaterialRepairResultDto { RepairId = previous.Id, AlreadyApplied = true };
        }
        var planned = await BuildPlan(batchId, kind);
        if (planned.IsFailure) return planned.Error;
        var plan = planned.Value;
        if (!plan.Preview.HasChanges && plan.Preview.Blockers.Count == 0)
            return new IssuedMaterialRepairResultDto { AlreadyApplied = true };
        if (plan.Preview.PreviewToken != request.PreviewToken)
            return Error.Conflict("Repair.StalePreview", "The master, issue or stock changed. Reload and review a fresh preview.");
        if (!plan.Preview.CanApply)
            return Error.Conflict("Repair.Blocked", string.Join(" ", plan.Preview.Blockers));
        try
        {
            var audit = await Post(plan, request);
            access = await Access(batchId, kind);
            if (access.IsFailure)
            {
                db.ChangeTracker.Clear();
                return access.Error;
            }
            db.IssuedMaterialRepairs.Add(audit);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return new IssuedMaterialRepairResultDto { RepairId = audit.Id, Applied = true };
        }
        catch
        {
            // A later audit writer using this scoped context must never flush a refused or
            // rolled-back stock plan outside its transaction.
            try { await transaction.RollbackAsync(); }
            finally { db.ChangeTracker.Clear(); }
            throw;
        }
    }

    private static string Token(object snapshot) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(snapshot))));
}
