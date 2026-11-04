using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products.Production;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;

namespace APP.Services.Production;

public sealed partial class ProductionUnstartService(ApplicationDbContext db, ICurrentUserService identity)
{
    private async Task<Result<Guid>> Access(Guid activityId)
    {
        var grants = await CurrentPermissionSet.Load(db, identity.UserId);
        var department = await db.Users.IgnoreAutoIncludes().Where(user => user.Id == identity.UserId && !user.IsDisabled
            && user.Department.Type == DepartmentType.Production).Select(user => user.DepartmentId).SingleOrDefaultAsync();
        if (department is null || !grants.Contains(PermissionKeys.CanViewPlannedProducts) || !grants.Contains(PermissionKeys.CanUnstartProduction))
            return Error.Forbidden("Unstart.Permission", "Your current department and permissions do not allow this repair.");
        var batch = await db.ProductionActivities.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Where(run => run.Id == activityId && run.ProductionScheduleProduct.Product.DepartmentId == department
                && run.ProductionScheduleProduct.Product.DeletedAt == null).Select(run => (Guid?)run.ProductionScheduleProductId).SingleOrDefaultAsync();
        return batch.HasValue ? batch.Value : Error.NotFound("Unstart.Activity", "Production run not found in your department.");
    }

    public async Task<Result<ProductionUnstartPreviewDto>> Preview(Guid activityId)
    {
        var access = await Access(activityId);
        if (access.IsFailure) return access.Error;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        var plan = await Build(activityId);
        return plan.IsSuccess ? plan.Value.Preview : plan.Error;
    }

    public async Task<Result<ProductionUnstartResultDto>> Apply(Guid activityId, ProductionUnstartRequest request)
    {
        if (request.RequestId == Guid.Empty || request.PreviewToken.Length != 64 || request.Reason.Trim().Length is < 3 or > 1000)
            return Error.Validation("Unstart.Request", "A request ID, current preview and reason (3–1000 characters) are required.");
        var access = await Access(activityId);
        if (access.IsFailure) return access.Error;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '5s'");
            await LockEvidence();
            access = await Access(activityId);
            if (access.IsFailure) return access.Error;
            var replay = await db.ProductionUnstartRepairs.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.RequestId)
                ?? await db.ProductionUnstartRepairs.AsNoTracking().SingleOrDefaultAsync(item => item.ActivityId == activityId);
            if (replay is not null)
            {
                if (replay.Id == request.RequestId && (replay.ActivityId != activityId || replay.ActorId != identity.UserId
                    || replay.PreviewToken != request.PreviewToken || replay.Reason != request.Reason.Trim()))
                    return Error.Conflict("Unstart.RequestReused", "This request ID belongs to a different repair. Reload the preview.");
                return new ProductionUnstartResultDto { RepairId = replay.Id, BatchId = replay.BatchId,
                    ScheduleId = await db.ProductionScheduleProducts.IgnoreQueryFilters().Where(item => item.Id == replay.BatchId).Select(item => item.ProductionScheduleId).SingleAsync(), AlreadyApplied = true };
            }
            var result = await Build(activityId);
            if (result.IsFailure) return result.Error;
            var plan = result.Value;
            if (plan.Preview.PreviewToken != request.PreviewToken)
                return Error.Conflict("Unstart.StalePreview", "The run, documents or reservations changed. Reload and review a fresh preview.");
            if (!plan.Preview.CanApply) return Error.Conflict("Unstart.Blocked", string.Join(" ", plan.Preview.Blockers));
            access = await Access(activityId);
            if (access.IsFailure) return access.Error;
            var before = Snapshot(plan);
            var now = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(plan.Activity.ProductionScheduleProduct.BatchNumber) && !string.IsNullOrWhiteSpace(plan.Preview.BatchNumber))
                plan.Activity.ProductionScheduleProduct.BatchNumber = plan.Preview.BatchNumber;
            plan.Activity.DeletedAt = now;
            plan.Activity.LastDeletedById = identity.UserId;
            foreach (var step in plan.Steps) { step.DeletedAt = now; step.LastDeletedById = identity.UserId; }
            foreach (var record in plan.Bmrs) { record.DeletedAt = now; record.LastDeletedById = identity.UserId; }
            foreach (var record in plan.Bprs) { record.DeletedAt = now; record.LastDeletedById = identity.UserId; }
            foreach (var reservation in plan.Reservations) { reservation.DeletedAt = now; reservation.ReleasedAt = now; reservation.LastDeletedById = identity.UserId; reservation.ProductionUnstartRepairId = request.RequestId; }
            foreach (var configuration in plan.Configurations) { configuration.DeletedAt = now; configuration.LastDeletedById = identity.UserId; }
            plan.Schedule.Status = (ProductionStatus)plan.Preview.ScheduleStatusAfter;
            db.ProductionActivityLogs.Add(new() { ProductionActivityId = activityId, UserId = identity.UserId, Timestamp = now,
                Message = $"Production unstarted before BMR issue. Repair {request.RequestId}." });
            await db.SaveChangesAsync();
            access = await Access(activityId);
            if (access.IsFailure)
            {
                await transaction.RollbackAsync(); db.ChangeTracker.Clear(); return access.Error;
            }
            db.ProductionUnstartRepairs.Add(new() { Id = request.RequestId, ActivityId = activityId, BatchId = plan.Activity.ProductionScheduleProductId,
                ActorId = identity.UserId.GetValueOrDefault(), AppliedAt = now, Reason = request.Reason.Trim(), PreviewToken = request.PreviewToken,
                BeforeJson = before, AfterJson = Snapshot(plan) });
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return new ProductionUnstartResultDto { RepairId = request.RequestId, BatchId = plan.Preview.BatchId, ScheduleId = plan.Schedule.Id };
        }
        catch
        {
            try { await transaction.RollbackAsync(); }
            finally { db.ChangeTracker.Clear(); }
            throw;
        }
    }

    private static string Hash(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
