using DOMAIN.Entities.Materials.Batch;

namespace APP.Services.Production;

/// <summary>Release an unused hold without recording stock consumption or changing physical/QC stock state.</summary>
internal static class UnstartReservationRelease
{
    public static void Release(MaterialBatchReservedQuantity reservation, Guid unstartId, Guid actorId, DateTime now)
    {
        reservation.ReleasedAt = now;
        reservation.DeletedAt ??= now;
        reservation.LastDeletedById ??= actorId;
        reservation.ProductionUnstartRepairId = unstartId;
    }
}
