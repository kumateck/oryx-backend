using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using APP.IRepository;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static partial class ProductionScheduleRepairs
{
    internal static Guid BatchRequestId(Guid requestId, Guid batchId) =>
        new(SHA256.HashData(Encoding.UTF8.GetBytes($"{requestId:D}/{batchId:D}")).AsSpan(0, 16));

    internal static async Task<Result<ScheduleRepairResult>> Apply(ApplicationDbContext db, IApprovalRepository approvals,
        UserManager<User> users, Guid scheduleId, Guid productId, Guid actorId, ScheduleRepairKind kind, ApplyScheduleRepair request)
    {
        if (request.RequestId == Guid.Empty || request.PreviewHash?.Length != 64 || string.IsNullOrWhiteSpace(request.Reason)
            || request.Reason.Trim().Length is < 5 or > 2000)
            return Error.Validation("Repair.Request", "A reviewed preview, request ID and reason of 5–2000 characters are required.");
        var access = await Access(db, scheduleId, productId, actorId, kind);
        if (access.IsFailure) return access.Error;
        await using var transaction = await ProductionMaterialTransaction.Begin(db);
        var committed = false;
        try
        {
            access = await Access(db, scheduleId, productId, actorId, kind);
            if (access.IsFailure) return access.Error;
            var replay = await db.ProductionScheduleRepairs.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.RequestId);
            if (replay is not null)
            {
                if (replay.ScheduleId != scheduleId || replay.ProductId != productId || replay.ActorId != actorId || replay.Kind != kind
                    || replay.PreviewHash != request.PreviewHash || replay.Reason != request.Reason.Trim())
                    return Error.Conflict("Repair.RequestReused", "This request ID belongs to a different repair.");
                var saved = JsonSerializer.Deserialize<ScheduleRepairResult>(replay.ResultJson);
                if (saved is null) return Error.Conflict("Repair.Receipt", "The saved repair receipt could not be read.");
                saved.AlreadyApplied = true;
                return saved;
            }
            var plan = await Build(db, users, scheduleId, productId, actorId, kind);
            if (plan.Preview.PreviewHash != request.PreviewHash)
                return Error.Conflict("Repair.StalePreview", "The product, batches or stock changed. Reload and review the preview.");
            if (!plan.Preview.CanApply)
                return Error.Conflict("Repair.Blocked", "Resolve the blocked batches or review the current product configuration before applying.");
            var result = new ScheduleRepairResult { RepairId = request.RequestId,
                Skipped = plan.Preview.Batches.Count(row => row.Status == "skipped"),
                Unchanged = plan.Preview.Batches.Count(row => row.Status == "unchanged") };
            foreach (var batch in plan.Preview.Batches.Where(row => row.Status == "ready"))
            {
                access = await Access(db, scheduleId, productId, actorId, kind);
                if (access.IsFailure) return access.Error;
                if (batch.ActivityId is not { } activityId) return Error.Conflict("Repair.Activity", "The production run changed.");
                if (kind == ScheduleRepairKind.Procedure)
                {
                    var repaired = await ProductionProcedureRepair.RepairAsync(db, users, activityId, actorId);
                    if (repaired.IsFailure) return repaired.Error;
                    db.ProductionActivityLogs.Add(new() { ProductionActivityId = activityId, UserId = actorId, Timestamp = DateTime.UtcNow,
                        Message = $"Bulk procedure repair {request.RequestId}. The complete reason is recorded in the schedule repair audit." });
                    await db.SaveChangesAsync();
                }
                else
                {
                    var materialKind = kind == ScheduleRepairKind.Bom ? MaterialKind.Raw : MaterialKind.Package;
                    // Earlier batches can change the remaining free stock. Re-plan under the same
                    // locks; any later shortage refuses and rolls back the entire product repair.
                    var fresh = await ProductionMaterialRepairs.Preview(db, activityId, actorId, materialKind);
                    if (fresh.IsFailure) return fresh.Error;
                    var repaired = await ProductionMaterialRepairs.Apply(db, approvals, activityId, actorId, materialKind,
                        new ApplyProductionMaterialRepair { RequestId = BatchRequestId(request.RequestId, batch.BatchId),
                            PreviewHash = fresh.Value.PreviewHash, Reason = request.Reason.Trim() });
                    if (repaired.IsFailure) return repaired.Error;
                    if (repaired.Value.ApprovalRequired) result.RequiringApproval++;
                }
                result.Repaired++;
            }
            access = await Access(db, scheduleId, productId, actorId, kind);
            if (access.IsFailure) return access.Error;
            var after = await Build(db, users, scheduleId, productId, actorId, kind);
            db.ProductionScheduleRepairs.Add(new() { Id = request.RequestId, ScheduleId = scheduleId, ProductId = productId,
                ActorId = actorId, Kind = kind, AppliedAt = DateTime.UtcNow, Reason = request.Reason.Trim(), PreviewHash = request.PreviewHash,
                BeforeJson = ProductionMaterialSources.Json(plan), AfterJson = ProductionMaterialSources.Json(after),
                ResultJson = ProductionMaterialSources.Json(result) });
            await db.SaveChangesAsync();
            await transaction.CommitAsync(); committed = true;
            return result;
        }
        catch (DbUpdateException) { return Error.Conflict("Repair.Conflict", "The bulk repair could not be committed. No batches were changed. Reload the preview."); }
        catch (Npgsql.PostgresException) { return Error.Conflict("Repair.Busy", "Production data changed or is busy. No batches were changed. Reload the preview."); }
        finally
        {
            if (!committed) { await transaction.RollbackAsync(); db.ChangeTracker.Clear(); }
        }
    }
}
