using System.Data;
using System.Text.Json;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products.Production;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Production;

public sealed partial class UnstartReservationRepairService
{
    public async Task<Result<ReleaseUnstartReservationsResultDto>> Apply(Guid materialId, MaterialKind kind, ReleaseUnstartReservationsRequest request)
    {
        if (request.RequestId == Guid.Empty || request.PreviewToken?.Length != 64 || request.Reason?.Trim().Length is not (>= 3 and <= 1000)
            || request.ReservationIds is null || request.ReservationIds.Count is < 1 or > 200
            || request.ReservationIds.Contains(Guid.Empty) || request.ReservationIds.Distinct().Count() != request.ReservationIds.Count)
            return Error.Validation("UnstartReservations.Request", "Select reservations and provide a current preview, request ID and reason (3–1000 characters).");
        var access = await Access(materialId, kind, true);
        if (access.IsFailure) return access.Error;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '5s'");
            await db.Database.ExecuteSqlRawAsync("""
                LOCK TABLE "BinCardInformation", "MaterialBatchReservedQuantities", "ProductionActivities", "ProductionUnstartRepairs",
                "RequisitionItems", "Requisitions", "StockRequisitionIssues", "UnstartReservationRepairs" IN SHARE ROW EXCLUSIVE MODE
                """);
            access = await Access(materialId, kind, true);
            if (access.IsFailure) return access.Error;
            var ids = JsonSerializer.Serialize(request.ReservationIds.Order());
            var replay = await db.UnstartReservationRepairs.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.RequestId);
            if (replay is not null)
            {
                if (replay.ActorId != identity.UserId || replay.DepartmentId != access.Value || replay.MaterialId != materialId
                    || replay.PreviewToken != request.PreviewToken || replay.Reason != request.Reason.Trim() || replay.ReservationIdsJson != ids)
                    return Error.Conflict("UnstartReservations.RequestReused", "This request ID belongs to a different repair.");
                return new ReleaseUnstartReservationsResultDto { RepairId = replay.Id, ReleasedCount = request.ReservationIds.Count, AlreadyApplied = true };
            }
            var plan = await Build(materialId, access.Value, kind);
            if (plan.Preview.PreviewToken != request.PreviewToken)
                return Error.Conflict("UnstartReservations.Stale", "Reservations or production evidence changed. Review a fresh preview.");
            var selected = plan.Preview.Reservations.Where(row => request.ReservationIds.Contains(row.ReservationId)).ToList();
            if (selected.Count != request.ReservationIds.Count || selected.Any(row => row.Blockers.Count != 0))
                return Error.Conflict("UnstartReservations.Blocked", "Some selected reservations cannot be released. Review the blockers.");
            access = await Access(materialId, kind, true);
            if (access.IsFailure) return access.Error;
            var now = DateTime.UtcNow;
            foreach (var reservation in plan.Reservations.Where(r => request.ReservationIds.Contains(r.Id)))
            {
                var receipt = plan.Unstarts[reservation.Id];
                UnstartReservationRelease.Release(reservation, receipt.Id, identity.UserId!.Value, now);
                db.ProductionActivityLogs.Add(new() { ProductionActivityId = receipt.ActivityId, UserId = identity.UserId, Timestamp = now,
                    Message = $"Unused reservation {reservation.Id} released after Unstart {receipt.Id}. Repair {request.RequestId}." });
            }
            db.UnstartReservationRepairs.Add(new() { Id = request.RequestId, ActorId = identity.UserId!.Value, DepartmentId = access.Value,
                MaterialId = materialId, AppliedAt = now, Reason = request.Reason.Trim(), PreviewToken = request.PreviewToken,
                BeforeJson = plan.Snapshot, ReservationIdsJson = ids });
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return new ReleaseUnstartReservationsResultDto { RepairId = request.RequestId, ReleasedCount = selected.Count };
        }
        catch
        {
            try { await transaction.RollbackAsync(); }
            finally { db.ChangeTracker.Clear(); }
            throw;
        }
    }
}
