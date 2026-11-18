using System.Data;
using System.Text.Json;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Requisitions;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Production;

public sealed partial class UnstartLeftoverRepairService
{
    public async Task<Result<RepairUnstartLeftoversResultDto>> Apply(MaterialKind kind, RepairUnstartLeftoversRequest request)
    {
        if (request.RequestId == Guid.Empty || request.PreviewToken?.Length != 64 || request.Reason?.Trim().Length is not (>= 3 and <= 1000)
            || request.RequisitionIds is null || request.RequisitionIds.Count is < 1 or > 200
            || request.RequisitionIds.Contains(Guid.Empty) || request.RequisitionIds.Distinct().Count() != request.RequisitionIds.Count)
            return Error.Validation("UnstartLeftovers.Request", "Select requests and provide a current preview, request ID and reason (3–1000 characters).");
        var access = await Access(kind, true);
        if (access.IsFailure) return access.Error;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '5s'");
            // Serialize against restarts, new requests and stock issue writers before rebuilding eligibility.
            await db.Database.ExecuteSqlRawAsync("""
                LOCK TABLE "BinCardInformation", "MaterialBatchReservedQuantities", "ProductionActivities",
                "ProductionUnstartRepairs", "RequisitionItems", "Requisitions", "StockRequisitionIssues",
                "UnstartLeftoverRepairs" IN SHARE ROW EXCLUSIVE MODE
                """);
            access = await Access(kind, true);
            if (access.IsFailure) return access.Error;
            var idsJson = JsonSerializer.Serialize(request.RequisitionIds.Order());
            var replay = await db.UnstartLeftoverRepairs.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.RequestId);
            if (replay is not null)
            {
                if (replay.ActorId != identity.UserId || replay.DepartmentId != access.Value || replay.Kind != kind
                    || replay.PreviewToken != request.PreviewToken || replay.Reason != request.Reason.Trim() || replay.RequisitionIdsJson != idsJson)
                    return Error.Conflict("UnstartLeftovers.RequestReused", "This request ID belongs to another repair. Review a fresh preview.");
                return new RepairUnstartLeftoversResultDto { RepairId = replay.Id, CancelledCount = request.RequisitionIds.Count, AlreadyApplied = true };
            }
            var plan = await Build(access.Value, kind);
            if (plan.Preview.PreviewToken != request.PreviewToken)
                return Error.Conflict("UnstartLeftovers.Stale", "The requests or production evidence changed. Review a fresh preview.");
            var selected = plan.Preview.Requisitions.Where(row => request.RequisitionIds.Contains(row.RequisitionId)).ToList();
            if (selected.Count != request.RequisitionIds.Count || selected.Any(row => row.Blockers.Count != 0))
                return Error.Conflict("UnstartLeftovers.Blocked", "Some selected requests cannot be repaired. Review the blockers in a fresh preview.");
            access = await Access(kind, true);
            if (access.IsFailure) return access.Error;
            var now = DateTime.UtcNow;
            foreach (var r in plan.Requests.Where(r => request.RequisitionIds.Contains(r.Id)))
            {
                r.DeletedAt = now; r.LastDeletedById = identity.UserId;
                foreach (var line in r.Items.Where(line => line.DeletedAt == null)) { line.DeletedAt = now; line.LastDeletedById = identity.UserId; }
                db.ProductionActivityLogs.Add(new() { ProductionActivityId = plan.Activities[r.Id], UserId = identity.UserId, Timestamp = now,
                    Message = $"Unstart leftover request {r.Code ?? r.Id.ToString()} cancelled. Repair {request.RequestId}." });
            }
            db.UnstartLeftoverRepairs.Add(new() { Id = request.RequestId, ActorId = identity.UserId!.Value, DepartmentId = access.Value,
                Kind = kind, AppliedAt = now, Reason = request.Reason.Trim(), PreviewToken = request.PreviewToken,
                BeforeJson = plan.Snapshot, RequisitionIdsJson = idsJson });
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return new RepairUnstartLeftoversResultDto { RepairId = request.RequestId, CancelledCount = selected.Count };
        }
        catch
        {
            try { await transaction.RollbackAsync(); }
            finally { db.ChangeTracker.Clear(); }
            throw;
        }
    }
}
