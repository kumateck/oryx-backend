using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Materials.StockMigrations;
using DOMAIN.Entities.Products.Production;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;

namespace APP.Services.Production;

public sealed partial class ReservationRepairService(ApplicationDbContext db, ICurrentUserService identity)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private sealed record AccessScope(Guid DepartmentId, string BatchNumber);
    private sealed record Plan(string Snapshot, ReservationRepairPreviewDto Preview);

    private async Task<Result<AccessScope>> Access(Guid batchId)
    {
        var grants = await CurrentPermissionSet.Load(db, identity.UserId);
        var department = await db.Users.IgnoreAutoIncludes().Where(user => user.Id == identity.UserId
            && !user.IsDisabled && user.Department.Type == DepartmentType.Production)
            .Select(user => user.DepartmentId).SingleOrDefaultAsync();
        if (department is null || !grants.Contains(PermissionKeys.CanViewPlannedProducts)
            || !grants.Contains(PermissionKeys.CanRepairStockReservations))
            return Error.Forbidden("ReservationRepair.Permission", "Your current department and permissions do not allow reservation repair.");
        var batch = await db.ProductionScheduleProducts.IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.Id == batchId && item.Product.DepartmentId == department && item.Product.DeletedAt == null)
            .Select(item => new { item.BatchNumber }).SingleOrDefaultAsync();
        return batch is null ? Error.NotFound("ReservationRepair.Batch", "Production batch not found in your department.")
            : new AccessScope(department.Value, batch.BatchNumber ?? "");
    }

    public async Task<Result<ReservationRepairPreviewDto>> Preview(Guid batchId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        var access = await Access(batchId);
        if (access.IsFailure) return access.Error;
        var plan = await Build(batchId, access.Value);
        return plan.IsSuccess ? plan.Value.Preview : plan.Error;
    }

    public async Task<Result<ReservationRepairResultDto>> Apply(Guid batchId, ReservationRepairRequest request)
    {
        if (request.RequestId == Guid.Empty || !Regex.IsMatch(request.PreviewToken ?? "", "^[a-f0-9]{64}$")
            || (request.Reason ?? "").Trim().Length is < 10 or > 1000)
            return Error.Validation("ReservationRepair.Request", "A request ID, current preview and reason (10–1000 characters) are required.");
        var access = await Access(batchId);
        if (access.IsFailure) return access.Error;
        // Acquire locks before rebuilding the plan, so its statement snapshots include any writer
        // that committed while we waited. Ordinary issue/migration writers also conflict with these locks.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await LockEvidence();
            access = await Access(batchId);
            if (access.IsFailure) return access.Error;
            var previous = await db.ReservationRepairs.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.RequestId);
            if (previous is not null)
            {
                if (previous.BatchId != batchId || previous.ActorId != identity.UserId
                    || previous.PreviewToken != request.PreviewToken || previous.Reason != request.Reason.Trim())
                    return Error.Conflict("ReservationRepair.RequestReused", "This request ID belongs to a different repair. Reload the preview.");
                return new ReservationRepairResultDto { RepairId = previous.Id, AlreadyApplied = true };
            }
            var result = await Build(batchId, access.Value);
            if (result.IsFailure) return result.Error;
            var plan = result.Value;
            if (plan.Preview.PreviewToken != request.PreviewToken)
                return Error.Conflict("ReservationRepair.StalePreview", "Reservations, replacement stock or the waiting queue changed. Reload and review a fresh preview.");
            if (!plan.Preview.CanApply)
                return Error.Conflict("ReservationRepair.Blocked", plan.Preview.HasChanges
                    ? string.Join(" ", plan.Preview.Blockers) : "No affected reservations remain.");
            access = await Access(batchId);
            if (access.IsFailure) return access.Error;
            var after = await Rebind(plan.Preview.Rows.Count);
            var audit = new ReservationRepair { Id = request.RequestId, BatchId = batchId, ActorId = identity.UserId.GetValueOrDefault(),
                MigrationId = plan.Preview.MigrationId, AppliedAt = DateTime.UtcNow, Reason = request.Reason.Trim(),
                PreviewToken = request.PreviewToken, BeforeJson = plan.Snapshot, AfterJson = after,
                RowsJson = JsonSerializer.Serialize(plan.Preview.Rows, Json) };
            db.ReservationRepairs.Add(audit);
            var activity = await db.ProductionActivities.IgnoreAutoIncludes().SingleAsync(item => item.ProductionScheduleProductId == batchId);
            db.ProductionActivityLogs.Add(new() { ProductionActivityId = activity.Id, UserId = identity.UserId.GetValueOrDefault(),
                Timestamp = audit.AppliedAt, Message = $"Stock reservations repaired after opening-stock replacement. Repair {audit.Id}. Reason: {audit.Reason}" });
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return new ReservationRepairResultDto { RepairId = audit.Id };
        }
        catch
        {
            try { await transaction.RollbackAsync(); }
            finally { db.ChangeTracker.Clear(); }
            throw;
        }
    }

    private async Task<Result<Plan>> Build(Guid batchId, AccessScope access)
    {
        var migrations = await db.StockMigrations.IgnoreAutoIncludes().AsNoTracking().Where(item => item.DepartmentId == access.DepartmentId
                && item.Mode == StockMigrationMode.OpeningBalance && item.Status == StockMigrationStatus.Approved && item.ApprovedAt != null)
            .OrderByDescending(item => item.ApprovedAt).Take(2).Select(item => new { item.Id, item.FileName, item.ApprovedAt }).ToListAsync();
        if (migrations.Count == 0 || migrations.Count == 2 && migrations[0].ApprovedAt == migrations[1].ApprovedAt)
            return Error.Conflict("ReservationRepair.Migration", "Cannot identify one current approved replacement migration. Review the opening-stock records first.");
        var migration = migrations[0];
        var snapshot = await ReadPlan(batchId, migration.Id);
        using var document = JsonDocument.Parse(snapshot);
        var report = document.RootElement;
        var rows = report.GetProperty("rows").Deserialize<List<ReservationRepairRowDto>>(Json) ?? [];
        var blockers = report.GetProperty("problems").Deserialize<List<string>>(Json) ?? [];
        var history = await db.ReservationRepairs.AsNoTracking().Where(item => item.BatchId == batchId)
            .OrderByDescending(item => item.AppliedAt).ToListAsync();
        var actors = history.Select(item => item.ActorId).Distinct().ToList();
        var names = await db.Users.IgnoreQueryFilters().IgnoreAutoIncludes().Where(user => actors.Contains(user.Id))
            .Select(user => new { user.Id, user.UserName }).ToDictionaryAsync(user => user.Id, user => user.UserName ?? "");
        return new Plan(snapshot, new ReservationRepairPreviewDto { BatchId = batchId, BatchNumber = access.BatchNumber,
            MigrationId = migration.Id, MigrationFile = migration.FileName ?? "", Rows = rows, Blockers = blockers,
            HasChanges = rows.Count > 0, CanApply = rows.Count > 0 && blockers.Count == 0,
            PreviewToken = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot))),
            History = history.Select(item => new ReservationRepairHistoryDto { Id = item.Id, ActorId = item.ActorId,
                ActorName = names.GetValueOrDefault(item.ActorId, ""), AppliedAt = item.AppliedAt, Reason = item.Reason,
                Rows = JsonSerializer.Deserialize<List<ReservationRepairRowDto>>(item.RowsJson, Json) ?? [] }).ToList() });
    }
}
