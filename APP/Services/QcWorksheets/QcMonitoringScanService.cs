using DOMAIN.Entities.QcWorksheets;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace APP.Services.QcWorksheets;

/// <summary>
/// The daily due-date scan: turns monitoring programs that have come due into real testing
/// rounds.
/// <para>
/// A service rather than part of a repository, for the same reason
/// <see cref="IQcCoaGenerationService"/> is one: this is a system act with a scheduled trigger,
/// and it has to be independently testable without a hosted process or a clock.
/// </para>
/// </summary>
public interface IQcMonitoringScanService
{
    /// <summary>
    /// Runs one scan. <paramref name="asOf"/> is the day being scanned — supplied so the job can
    /// pass the real clock and a test can pass a fixed date, rather than the service reading
    /// <c>DateTime.UtcNow</c> out from under its own callers.
    /// <para>
    /// Idempotent within a day by construction: every program included in a generated round has
    /// its <see cref="MonitoringProgram.NextDueDate"/> advanced in the same transaction, so a
    /// second run the same day finds nothing due.
    /// </para>
    /// </summary>
    Task<MonitoringScanResultDto> RunAsync(DateTime asOf, Guid? userId, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public class QcMonitoringScanService(
    ApplicationDbContext context,
    ILogger<QcMonitoringScanService> logger) : IQcMonitoringScanService
{
    /// <summary>
    /// Guards the catch-up loop against a program left overdue for years with a Daily frequency.
    /// One scan advances a program at most this many intervals; anything beyond it is a
    /// configuration problem, not a schedule to replay.
    /// </summary>
    private const int MaxCatchUpIntervals = 1000;

    public async Task<MonitoringScanResultDto> RunAsync(
        DateTime asOf, Guid? userId, CancellationToken cancellationToken = default)
    {
        var today = asOf.Date;
        var result = new MonitoringScanResultDto { RanAt = asOf };

        // Step 1: every Active program inside its lead time. LeadTimeDays is per-program, so the
        // horizon cannot be a single constant in the query — the comparison is evaluated per row.
        var due = await context.QcMonitoringPrograms
            .Include(item => item.SamplingPoint)
            .Include(item => item.Specification)
                .ThenInclude(specification => specification.WorksheetLinks)
                    .ThenInclude(link => link.WorksheetTemplate)
            .Where(item => item.Status == MonitoringProgramStatus.Active)
            .Where(item => item.NextDueDate <= today.AddDays(item.LeadTimeDays))
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        result.ProgramsDue = due.Count;

        if (due.Count == 0)
        {
            result.WaterPeriodsExpired = await ExpireElapsedWaterPeriods(asOf, userId, cancellationToken);
            return result;
        }

        // Step 2: group by (the round type the point's discipline implies, specification version).
        // This is the whole correction this milestone makes — "one point per program" is about
        // configuration granularity, and a real round covers 60-90 rooms or 15+ water points in
        // one sweep. Programs that share a type and a specification belong in one round.
        var groups = due
            .Where(program => program.SamplingPoint is not null)
            .GroupBy(program => (
                Type: QcSamplingPointTypes.ToTestRequestType(program.SamplingPoint.Type),
                program.SpecificationId));

        foreach (var group in groups)
        {
            var eligible = new List<MonitoringProgram>();

            foreach (var program in group)
            {
                var skip = await SkipReason(program, cancellationToken);

                if (skip is null)
                {
                    eligible.Add(program);
                    continue;
                }

                // A skipped program's NextDueDate is deliberately NOT advanced: it stays visibly
                // Overdue in the calendar rather than quietly vanishing from the schedule.
                result.Skipped.Add(new MonitoringScanSkipDto
                {
                    MonitoringProgramId = program.Id,
                    SamplingPointCode = program.SamplingPoint?.Code,
                    Reason = skip
                });
            }

            if (eligible.Count == 0) continue;

            var specification = eligible[0].Specification;
            var testRequest = BuildRound(group.Key.Type, specification, eligible, asOf, userId);

            context.QcTestRequests.Add(testRequest);

            // Step 4: advance at generation time, not completion time — in the same transaction
            // that creates the round, so the same day's second run and tomorrow's run both find
            // nothing due for these programs while the round sits in the lab.
            foreach (var program in eligible)
            {
                program.NextDueDate = Advance(program, today);
                program.UpdatedAt = asOf;
                program.LastUpdatedById = userId;
            }

            result.ProgramsGenerated += eligible.Count;
            result.TestRequestsCreated++;
            result.CreatedTestRequestIds.Add(testRequest.Id);
        }

        await context.SaveChangesAsync(cancellationToken);

        result.WaterPeriodsExpired = await ExpireElapsedWaterPeriods(asOf, userId, cancellationToken);

        logger.LogInformation(
            "QC monitoring scan for {Date:yyyy-MM-dd}: {Due} due, {Generated} generated across "
            + "{Rounds} test request(s), {Skipped} skipped, {Expired} water period(s) expired.",
            today, result.ProgramsDue, result.ProgramsGenerated, result.TestRequestsCreated,
            result.Skipped.Count, result.WaterPeriodsExpired);

        return result;
    }

    // -----------------------------------------------------------------------
    // Step 3 — is this occurrence already covered?
    // -----------------------------------------------------------------------

    /// <summary>
    /// Why this due program is passed over, or null when it should be generated.
    /// <para>
    /// Two reasons, both deliberate.
    /// </para>
    /// <para>
    /// <b>An open round already covers the point.</b> The advance-at-generation rule already makes
    /// a same-day re-run a no-op, but a program whose <c>NextDueDate</c> was edited backwards, or
    /// whose round is still in the lab when the next occurrence comes due, would otherwise put the
    /// same point in two live rounds at once — two ARDs, two sets of worksheets, and no way to
    /// tell which one the certificate should draw on.
    /// </para>
    /// <para>
    /// <b>The pinned Specification is no longer Effective.</b> Hard version pinning means this
    /// program does not follow a revision on its own, so a superseded pin is a real configuration
    /// gap. Raising a round against a withdrawn controlled document would produce results judged
    /// by criteria that no longer apply, and a certificate printing a superseded specification
    /// code — so the scan refuses, leaves the program visibly Overdue, and says why.
    /// </para>
    /// </summary>
    private async Task<string> SkipReason(MonitoringProgram program, CancellationToken cancellationToken)
    {
        if (program.Specification is null)
            return "The specification this program is pinned to could not be resolved.";

        if (program.Specification.Status != QcDocumentStatus.Effective)
            return $"Specification '{program.Specification.Code}' is "
                   + $"{program.Specification.Status}, not Effective. Repoint the program at the "
                   + "current effective version.";

        if (program.Specification.WorksheetLinks.Count == 0)
            return $"Specification '{program.Specification.Code}' links no worksheet templates.";

        var alreadyOpen = await context.QcTestRequestSubjects.AnyAsync(
            subject =>
                subject.SamplingPointId == program.SamplingPointId
                && subject.TestRequest.SpecificationId == program.SpecificationId
                && subject.TestRequest.Status != TestRequestStatus.Released
                && subject.TestRequest.Status != TestRequestStatus.Rejected,
            cancellationToken);

        return alreadyOpen
            ? "An open test request already covers this sampling point against this specification."
            : null;
    }

    // -----------------------------------------------------------------------
    // Round construction
    // -----------------------------------------------------------------------

    /// <summary>
    /// One round, one Subject per due program, and one WorksheetInstance per
    /// (Subject × the pinned Specification's WorksheetLink) — the identical shape Milestone 3
    /// builds by hand, so a generated round is indistinguishable from a manually raised one
    /// downstream. No new structure is introduced for scheduled testing.
    /// </summary>
    private static TestRequest BuildRound(
        TestRequestType type,
        Specification specification,
        List<MonitoringProgram> programs,
        DateTime asOf,
        Guid? userId)
    {
        var testRequest = new TestRequest
        {
            Id = Guid.NewGuid(),
            Type = type,
            SpecificationId = specification.Id,

            // Pinned from the specification row, exactly as TestRequestRepository pins it. The
            // program's own pin is the same row, so the round inherits the version somebody
            // deliberately configured rather than whichever is Effective at 02:00.
            SpecificationVersion = specification.Version,
            ScheduleOrigin = TestRequestScheduleOrigin.Scheduled,

            // No UnscheduledReason: this round came from the schedule, which is the entire point
            // of the Scheduled origin. Milestone 3 rejects a reason on a scheduled round.
            ArNumber = BuildArNumber(type, asOf),
            IssuedAt = asOf,
            Status = TestRequestStatus.Draft,
            CreatedAt = asOf,
            CreatedById = userId
        };

        foreach (var program in programs.OrderBy(item => item.SamplingPoint.Code))
        {
            var point = program.SamplingPoint;
            var subjectId = Guid.NewGuid();

            testRequest.Subjects.Add(new TestRequestSubject
            {
                Id = subjectId,
                TestRequestId = testRequest.Id,

                // SubjectRef mirrors the point's code — it stays the display string, while
                // SamplingPointId is the real link. Milestone 6's whole correction is that the
                // string is no longer the only thing recorded.
                SubjectRef = point.Code,
                SubjectLabel = point.Name,
                SamplingPointId = point.Id,

                // From the point's own group, so the Alert/Action tier a result is judged against
                // is resolved from master data rather than typed per round.
                SamplingPointGroupId = point.SamplingPointGroupId,
                CreatedAt = asOf,
                CreatedById = userId,
                WorksheetInstances = specification.WorksheetLinks
                    .OrderBy(link => link.AnalysisType)
                    .Select(link => new WorksheetInstance
                    {
                        Id = Guid.NewGuid(),
                        TestRequestSubjectId = subjectId,
                        WorksheetTemplateId = link.WorksheetTemplateId,

                        // The link's own pin, copied straight across — never re-read from
                        // whichever template version is Effective now.
                        WorksheetTemplateVersion = link.WorksheetTemplateVersion > 0
                            ? link.WorksheetTemplateVersion
                            : link.WorksheetTemplate?.Version ?? 0,
                        AnalysisType = link.AnalysisType,
                        Status = WorksheetInstanceStatus.NotStarted,
                        Approved = false,
                        CreatedAt = asOf,
                        CreatedById = userId
                    })
                    .ToList()
            });
        }

        return testRequest;
    }

    /// <summary>
    /// A readable round-level AR number for a system-raised round. Deliberately not a sequence
    /// off a counter table: this module allocates no numbers anywhere else either, and a
    /// collision-free date-plus-suffix is enough for a round nobody types by hand.
    /// </summary>
    private static string BuildArNumber(TestRequestType type, DateTime asOf)
    {
        var prefix = type == TestRequestType.RoutineWater ? "WTR" : "EM";
        return $"AR/{prefix}/{asOf:yyyyMMdd}/{Guid.NewGuid().ToString()[..4].ToUpperInvariant()}";
    }

    // -----------------------------------------------------------------------
    // The schedule itself
    // -----------------------------------------------------------------------

    /// <summary>
    /// The program's next due date after this generation.
    /// <para>
    /// Advanced from its <i>own</i> current due date rather than from today, so a program raised
    /// early inside its lead time does not drift later every cycle. A program that is genuinely
    /// overdue then keeps stepping until it lands in the future — one round is generated for the
    /// missed stretch, not one per missed occurrence, because replaying a month of un-run daily
    /// tests as thirty rounds would be fiction.
    /// </para>
    /// </summary>
    internal static DateTime Advance(MonitoringProgram program, DateTime today)
    {
        var next = Step(program, program.NextDueDate);

        for (var guard = 0; next.Date <= today && guard < MaxCatchUpIntervals; guard++)
            next = Step(program, next);

        return next;
    }

    private static DateTime Step(MonitoringProgram program, DateTime from) => program.Frequency switch
    {
        MonitoringFrequency.Daily => from.AddDays(1),
        MonitoringFrequency.Weekly => from.AddDays(7),
        MonitoringFrequency.Monthly => from.AddMonths(1),
        MonitoringFrequency.Quarterly => from.AddMonths(3),

        // Validated non-null and positive whenever Frequency is Custom; the fallback keeps this
        // total for a row that somehow reached storage without one, rather than looping for ever
        // on a zero-day interval.
        MonitoringFrequency.Custom => from.AddDays(
            program.CustomIntervalDays is > 0 ? program.CustomIntervalDays.Value : 1),
        _ => from.AddDays(1)
    };

    // -----------------------------------------------------------------------
    // Water windows that have run out
    // -----------------------------------------------------------------------

    /// <summary>
    /// Moves Active water windows whose <see cref="WaterQualityPeriod.ValidUntil"/> has passed to
    /// <see cref="WaterQualityPeriodStatus.Expired"/>.
    /// <para>
    /// The dynamic-ValidUntil rule requires a delayed next round to <i>actively</i> flag the old
    /// window as no-longer-current rather than silently keep covering production
    /// (lifecycle-and-governance.md, "Water validity periods"). Deriving expiry on read would
    /// leave the stored status saying Active, and anything reading the status directly — a
    /// production-side check, a report — would be told the water is still covered. So it is
    /// written, by the one job that already runs daily.
    /// </para>
    /// <para>
    /// Expiry deliberately does not cascade to <see cref="WaterUseRecord"/>s. A use recorded while
    /// the window was valid stays clean; expiry means "no new use is covered", not "what already
    /// happened is suspect". Only a hold — an actual quality signal — flags the uses underneath.
    /// </para>
    /// </summary>
    private async Task<int> ExpireElapsedWaterPeriods(
        DateTime asOf, Guid? userId, CancellationToken cancellationToken)
    {
        var elapsed = await context.QcWaterQualityPeriods
            .Where(item => item.Status == WaterQualityPeriodStatus.Active)
            .Where(item => item.ValidUntil.HasValue && item.ValidUntil.Value < asOf)
            .ToListAsync(cancellationToken);

        if (elapsed.Count == 0) return 0;

        foreach (var period in elapsed)
        {
            period.Status = WaterQualityPeriodStatus.Expired;
            period.UpdatedAt = asOf;
            period.LastUpdatedById = userId;
        }

        await context.SaveChangesAsync(cancellationToken);
        return elapsed.Count;
    }
}
