using DOMAIN.Entities.QcWorksheets;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace APP.Services.QcWorksheets;

/// <summary>
/// Scaffolds water validity windows when a Water certificate is issued.
/// <para>
/// A service rather than part of <c>CoaRepository</c>, for the same reason certificate generation
/// is one: this is a system trigger hanging off someone else's workflow, and it has to be
/// testable on its own.
/// </para>
/// </summary>
public interface IQcWaterQualityPeriodService
{
    /// <summary>
    /// Creates a <see cref="WaterQualityPeriodStatus.PendingActivation"/> window for each Subject
    /// of a <see cref="TestRequestType.RoutineWater"/> round whose certificate has just been
    /// issued. Returns how many were created — zero is an ordinary outcome, since this is called
    /// speculatively after every issuance regardless of round type.
    /// <para>
    /// The window covers nothing until somebody activates it. That is deliberate: see
    /// <see cref="WaterQualityPeriod"/>.
    /// </para>
    /// </summary>
    Task<int> ScaffoldForIssuedCoaAsync(Guid testRequestId, Guid userId);
}

/// <inheritdoc />
public class QcWaterQualityPeriodService(
    ApplicationDbContext context,
    ILogger<QcWaterQualityPeriodService> logger) : IQcWaterQualityPeriodService
{
    public async Task<int> ScaffoldForIssuedCoaAsync(Guid testRequestId, Guid userId)
    {
        if (testRequestId == Guid.Empty) return 0;

        var testRequest = await context.QcTestRequests
            .Include(item => item.Subjects)
            .SingleOrDefaultAsync(item => item.Id == testRequestId);

        // Water only. An Environmental round reports on a room; it certifies nothing that
        // production then consumes for a stretch of time, so it has no validity window.
        if (testRequest is null || testRequest.Type != TestRequestType.RoutineWater) return 0;

        var created = 0;

        foreach (var subject in testRequest.Subjects)
        {
            // A Subject with no sampling point cannot have a window: the window's ValidUntil is
            // resolved through the point's own monitoring program, and a window not attached to a
            // point covers nothing identifiable. An unscheduled Water round naming a point that
            // has no master-data row yet lands here.
            if (!subject.SamplingPointId.HasValue)
            {
                logger.LogInformation(
                    "Water certificate issued for test request {TestRequestId}, but subject "
                    + "{SubjectId} ('{SubjectRef}') names no sampling point, so no water quality "
                    + "period was created for it.",
                    testRequestId, subject.Id, subject.SubjectRef);
                continue;
            }

            // ValidFrom is always the sample's own collection time — the retroactive rule. With no
            // collection recorded there is no honest start for the window, and defaulting it to
            // "now" would claim coverage starting after production had already used the water.
            if (!subject.CollectedAt.HasValue)
            {
                logger.LogWarning(
                    "Water certificate issued for test request {TestRequestId}, but subject "
                    + "{SubjectId} ('{SubjectRef}') has no collection timestamp, so its water "
                    + "quality period has no valid start and was not created.",
                    testRequestId, subject.Id, subject.SubjectRef);
                continue;
            }

            // Idempotent: reissuing a revised certificate for the same round must not scaffold a
            // second window over the same Subject's results.
            var exists = await context.QcWaterQualityPeriods
                .AnyAsync(item => item.TestRequestSubjectId == subject.Id);

            if (exists) continue;

            context.QcWaterQualityPeriods.Add(new WaterQualityPeriod
            {
                Id = Guid.NewGuid(),
                SamplingPointId = subject.SamplingPointId.Value,
                TestRequestSubjectId = subject.Id,
                ValidFrom = subject.CollectedAt.Value,

                // No ValidUntil and no RetrospectiveReason yet. Both arrive at activation, from
                // the person who decides the backdated coverage is justified.
                ValidUntil = null,
                Status = WaterQualityPeriodStatus.PendingActivation,
                CreatedAt = DateTime.UtcNow,
                CreatedById = userId
            });

            created++;
        }

        if (created == 0) return 0;

        await context.SaveChangesAsync();

        logger.LogInformation(
            "Scaffolded {Count} water quality period(s) at PendingActivation for test request "
            + "{TestRequestId}. None covers production until explicitly activated.",
            created, testRequestId);

        return created;
    }
}
