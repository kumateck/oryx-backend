using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// Milestone 6's water acceptance criteria (4, 5, 6 and 7), end to end.
/// <para>
/// No test here creates a <see cref="WaterQualityPeriod"/> by hand. Every window in this file
/// exists because a Water round was scheduled by the scan, tested, reviewed, certified and
/// issued — which is the only way one ever appears in production.
/// </para>
/// </summary>
public class WaterQualityAcceptanceTests
{
    private const string MicrobialField = "tamc";

    /// <summary>A scheduled Water round taken all the way to an issued certificate.</summary>
    private sealed record IssuedWaterRound(
        QcWorksheetTestContext Harness,
        Guid TestRequestId,
        MonitoringProgram Program,
        SamplingPoint Point,
        User Analyst,
        DateTime CollectedAt);

    /// <summary>
    /// Runs the whole production path: seed a point and its schedule, let the scan raise the
    /// round, record sampling, fill in and submit each worksheet, review it, and issue the
    /// certificate the strict-hold gate produces.
    /// </summary>
    private static async Task<IssuedWaterRound> ArrangeIssuedWaterRound(
        QcWorksheetTestContext harness, params string[] pointCodes)
    {
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var template = await harness.SeedEffectiveTemplate(
            $"WS/MICRO/{Guid.NewGuid().ToString()[..6]}", WorksheetCategory.Microbial, MicrobialField);

        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RoutineWater, (template, SpecificationAnalysisType.Microbial));

        var characteristic = await harness.SeedCharacteristic(
            specification, template, MicrobialField, "NMT 100 CFU/mL", testName: "TAMC");

        characteristic.GroupName = "MICROBIAL";
        characteristic.DisplayOrder = 1;
        await harness.Db.SaveChangesAsync();

        var codes = pointCodes.Length == 0 ? ["WP-01"] : pointCodes;

        var today = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);

        SamplingPoint firstPoint = null;
        MonitoringProgram firstProgram = null;

        foreach (var code in codes)
        {
            var point = await harness.SeedSamplingPoint(code);

            // Weekly, so the next due date after generation is a full week out — a value that
            // could not be mistaken for a fixed offset from the sample's collection time.
            var program = await harness.SeedMonitoringProgram(
                point, specification, today, MonitoringFrequency.Weekly);

            firstPoint ??= point;
            firstProgram ??= program;
        }

        var scan = await harness.MonitoringScan.RunAsync(today, null);
        Assert.Equal(1, scan.TestRequestsCreated);

        var testRequestId = scan.CreatedTestRequestIds[0];

        // Incubation lag made real: the sample was taken four days before the certificate is
        // issued, which is exactly the window ValidFrom has to backdate over.
        var collectedAt = today.AddDays(-4);

        var sampled = await harness.TestRequests.RecordSample(
            testRequestId,
            new RecordTestRequestSampleRequest { CollectedAt = collectedAt },
            Guid.NewGuid());

        Assert.True(sampled.IsSuccess, sampled.Error?.Description);

        var analyst = await harness.SeedUser($"analyst.{Guid.NewGuid().ToString()[..6]}");

        foreach (var subject in sampled.Value.Subjects)
        {
            var instanceId = subject.WorksheetInstances.Single().Id;

            await harness.WorksheetInstances.Assign(
                instanceId,
                new AssignWorksheetInstanceRequest { AssignedToId = analyst.Id },
                Guid.NewGuid());

            await harness.WorksheetInstances.Start(instanceId, analyst.Id);

            await harness.WorksheetInstances.SaveValues(
                instanceId,
                new SaveWorksheetValuesRequest
                {
                    FieldValues =
                        [new WorksheetFieldValueEntry { FieldKey = MicrobialField, Value = "12 CFU/mL" }]
                },
                analyst.Id);

            var submitted = await harness.WorksheetInstances.Submit(instanceId, analyst.Id);
            Assert.True(submitted.IsSuccess, submitted.Error?.Description);

            var reviewed = await harness.WorksheetInstances.Review(
                instanceId,
                new ReviewWorksheetInstanceRequest
                {
                    Approve = true,
                    Password = QcWorksheetTestContext.CorrectPassword,
                    Comments = "Reviewed."
                },
                harness.Approver.Id,
                [harness.ApproverRole.Id]);

            Assert.True(reviewed.IsSuccess, reviewed.Error?.Description);
        }

        var certificate = await harness.Db.Coas
            .AsNoTracking()
            .SingleAsync(item => item.TestRequestId == testRequestId);

        var issued = await harness.Coas.Issue(certificate.Id, harness.Approver.Id);
        Assert.True(issued.IsSuccess, issued.Error?.Description);

        return new IssuedWaterRound(
            harness, testRequestId, firstProgram, firstPoint, analyst, collectedAt);
    }

    private static Task<List<WaterQualityPeriod>> PeriodsOf(QcWorksheetTestContext harness) =>
        harness.Db.QcWaterQualityPeriods.AsNoTracking().ToListAsync();

    // =======================================================================
    // Criterion 4 — a window starts PendingActivation, not Active
    // =======================================================================

    /// <summary>
    /// Criterion 4. Issuing a Water certificate scaffolds a window per Subject at
    /// <see cref="WaterQualityPeriodStatus.PendingActivation"/> with no <c>ValidUntil</c> —
    /// production cannot yet rely on it.
    /// <para>
    /// The gap is the point. <c>ValidFrom</c> is the sample's collection time, days before the
    /// certificate exists, so a window that activated itself on issuance would silently assert
    /// coverage over a stretch of production nobody had looked at.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Issuing_a_water_certificate_scaffolds_a_pending_window_per_subject()
    {
        using var harness = new QcWorksheetTestContext();
        var round = await ArrangeIssuedWaterRound(harness, "WP-01", "WP-02");

        var periods = await PeriodsOf(harness);
        Assert.Equal(2, periods.Count);

        Assert.All(periods, period =>
        {
            Assert.Equal(WaterQualityPeriodStatus.PendingActivation, period.Status);
            Assert.Null(period.ValidUntil);
            Assert.Null(period.ActivatedAt);
            Assert.Null(period.ActivatedById);

            // No system-authored placeholder reason: the field's whole value is that a person
            // wrote it, and no person has been asked anything yet.
            Assert.True(string.IsNullOrWhiteSpace(period.RetrospectiveReason));

            // Always the sample's own collection time — retroactive by construction.
            Assert.Equal(round.CollectedAt, period.ValidFrom);
        });

        // Each window is tied to the point through its Subject's real FK, not a code.
        var subjects = await harness.Db.QcTestRequestSubjects
            .AsNoTracking()
            .Where(item => item.TestRequestId == round.TestRequestId)
            .ToListAsync();

        Assert.All(periods, period =>
            Assert.Equal(
                subjects.Single(subject => subject.Id == period.TestRequestSubjectId).SamplingPointId,
                period.SamplingPointId));
    }

    /// <summary>
    /// An Environmental certificate scaffolds nothing. An EM round reports on a room; it certifies
    /// nothing production then consumes over a window of time.
    /// </summary>
    [Fact]
    public async Task An_environmental_certificate_creates_no_water_window()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var template = await harness.SeedEffectiveTemplate(
            $"WS/MICRO/{Guid.NewGuid().ToString()[..6]}", WorksheetCategory.Microbial, MicrobialField);

        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RoutineEnvironmental, (template, SpecificationAnalysisType.Microbial));

        var characteristic = await harness.SeedCharacteristic(
            specification, template, MicrobialField, "NMT 100 CFU/4Hrs", testName: "TAMC");

        characteristic.GroupName = "MICROBIAL";
        await harness.Db.SaveChangesAsync();

        var today = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);
        var room = await harness.SeedSamplingPoint("RM-01", SamplingPointType.Environmental);
        await harness.SeedMonitoringProgram(room, specification, today);

        var scan = await harness.MonitoringScan.RunAsync(today, null);
        var testRequestId = scan.CreatedTestRequestIds[0];

        await harness.TestRequests.RecordSample(
            testRequestId,
            new RecordTestRequestSampleRequest { CollectedAt = today.AddDays(-2) },
            Guid.NewGuid());

        var analyst = await harness.SeedUser($"analyst.{Guid.NewGuid().ToString()[..6]}");
        var detail = await harness.TestRequests.GetTestRequest(testRequestId);
        var instanceId = detail.Value.Subjects.Single().WorksheetInstances.Single().Id;

        await harness.WorksheetInstances.Assign(
            instanceId, new AssignWorksheetInstanceRequest { AssignedToId = analyst.Id }, Guid.NewGuid());

        await harness.WorksheetInstances.Start(instanceId, analyst.Id);

        await harness.WorksheetInstances.SaveValues(
            instanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues = [new WorksheetFieldValueEntry { FieldKey = MicrobialField, Value = "8 CFU/4Hrs" }]
            },
            analyst.Id);

        await harness.WorksheetInstances.Submit(instanceId, analyst.Id);

        await harness.WorksheetInstances.Review(
            instanceId,
            new ReviewWorksheetInstanceRequest
            {
                Approve = true,
                Password = QcWorksheetTestContext.CorrectPassword,
                Comments = "Reviewed."
            },
            harness.Approver.Id,
            [harness.ApproverRole.Id]);

        var certificate = await harness.Db.Coas.AsNoTracking()
            .SingleAsync(item => item.TestRequestId == testRequestId);

        var issued = await harness.Coas.Issue(certificate.Id, harness.Approver.Id);
        Assert.True(issued.IsSuccess, issued.Error?.Description);
        Assert.Equal(CoaCertificateShape.EnvironmentalMonitoringReport, issued.Value.CertificateShape);

        Assert.Empty(await PeriodsOf(harness));
    }

    // =======================================================================
    // Criterion 5 — activation needs a reason and a dynamic ValidUntil
    // =======================================================================

    /// <summary>
    /// Criterion 5. Activation without a reason is refused; with one, <c>ValidUntil</c> equals the
    /// point's <c>MonitoringProgram.NextDueDate</c> as it stands at that moment — not a fixed
    /// offset from <c>ValidFrom</c>.
    /// <para>
    /// The dynamic value is asserted twice over: once against the schedule's actual next due date,
    /// and once by moving that due date and showing the window follows it. A fixed-duration
    /// implementation would pass the first check and fail the second.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Activation_requires_a_reason_and_takes_valid_until_from_the_live_schedule()
    {
        using var harness = new QcWorksheetTestContext();
        var round = await ArrangeIssuedWaterRound(harness);

        var period = Assert.Single(await PeriodsOf(harness));

        var noReason = await harness.WaterQuality.Activate(
            period.Id, new ActivateWaterQualityPeriodRequest(), Guid.NewGuid());

        Assert.False(noReason.IsSuccess);
        Assert.Equal("QcWaterQualityPeriod.RetrospectiveReasonRequired", noReason.Error.Code);

        var blank = await harness.WaterQuality.Activate(
            period.Id,
            new ActivateWaterQualityPeriodRequest { RetrospectiveReason = "   " },
            Guid.NewGuid());

        Assert.False(blank.IsSuccess);
        Assert.Equal("QcWaterQualityPeriod.RetrospectiveReasonRequired", blank.Error.Code);

        // Still inert after the refusals.
        Assert.Equal(
            WaterQualityPeriodStatus.PendingActivation,
            (await harness.Db.QcWaterQualityPeriods.AsNoTracking()
                .SingleAsync(item => item.Id == period.Id)).Status);

        // The schedule's next due date as it now stands — a week past the scan, because the scan
        // advanced it at generation time.
        var expected = (await harness.Db.QcMonitoringPrograms.AsNoTracking()
            .SingleAsync(item => item.Id == round.Program.Id)).NextDueDate;

        var activator = await harness.SeedUser($"qa.{Guid.NewGuid().ToString()[..6]}");

        var activated = await harness.WaterQuality.Activate(
            period.Id,
            new ActivateWaterQualityPeriodRequest
            {
                RetrospectiveReason = "Results within limits; incubation window reviewed and cleared."
            },
            activator.Id);

        Assert.True(activated.IsSuccess, activated.Error?.Description);
        Assert.Equal(WaterQualityPeriodStatus.Active, activated.Value.Status);
        Assert.Equal(expected, activated.Value.ValidUntil);
        Assert.Equal(round.CollectedAt, activated.Value.ValidFrom);
        Assert.NotNull(activated.Value.ActivatedAt);
        Assert.Equal(activator.Id, activated.Value.ActivatedBy.Id);
        Assert.Contains("incubation window", activated.Value.RetrospectiveReason);

        // Not a fixed offset from ValidFrom: ValidFrom is four days before the scan, so a fixed
        // window would have landed somewhere else entirely.
        Assert.NotEqual(round.CollectedAt.AddDays(7), activated.Value.ValidUntil);

        // Already Active — activating twice is refused rather than re-stamped.
        var again = await harness.WaterQuality.Activate(
            period.Id,
            new ActivateWaterQualityPeriodRequest { RetrospectiveReason = "Again." },
            activator.Id);

        Assert.False(again.IsSuccess);
        Assert.Equal("QcWaterQualityPeriod.ActivateRequiresPendingActivation", again.Error.Code);
    }

    /// <summary>
    /// The second half of the dynamic rule: the window follows the schedule. Rescheduling the
    /// point's next test before activation moves <c>ValidUntil</c> with it, which a fixed duration
    /// from <c>ValidFrom</c> could never do.
    /// </summary>
    [Fact]
    public async Task Valid_until_follows_the_schedule_rather_than_a_fixed_duration()
    {
        using var harness = new QcWorksheetTestContext();
        var round = await ArrangeIssuedWaterRound(harness);

        var program = await harness.Db.QcMonitoringPrograms.SingleAsync(
            item => item.Id == round.Program.Id);

        // The next round slips by three days.
        var rescheduled = program.NextDueDate.AddDays(3);
        program.NextDueDate = rescheduled;
        await harness.Db.SaveChangesAsync();

        var period = Assert.Single(await PeriodsOf(harness));

        var activated = await harness.WaterQuality.Activate(
            period.Id,
            new ActivateWaterQualityPeriodRequest { RetrospectiveReason = "Cleared." },
            Guid.NewGuid());

        Assert.True(activated.IsSuccess, activated.Error?.Description);
        Assert.Equal(rescheduled, activated.Value.ValidUntil);
    }

    /// <summary>
    /// A point with no active schedule has no next test to bound a window against, and is refused
    /// rather than given an open-ended one — an unbounded window covering production indefinitely
    /// is the exact failure the dynamic rule exists to prevent.
    /// </summary>
    [Fact]
    public async Task A_point_with_no_active_schedule_cannot_have_a_window_activated()
    {
        using var harness = new QcWorksheetTestContext();
        var round = await ArrangeIssuedWaterRound(harness);

        var program = await harness.Db.QcMonitoringPrograms.SingleAsync(
            item => item.Id == round.Program.Id);

        program.Status = MonitoringProgramStatus.Paused;
        await harness.Db.SaveChangesAsync();

        var period = Assert.Single(await PeriodsOf(harness));

        var activated = await harness.WaterQuality.Activate(
            period.Id,
            new ActivateWaterQualityPeriodRequest { RetrospectiveReason = "Cleared." },
            Guid.NewGuid());

        Assert.False(activated.IsSuccess);
        Assert.Equal("QcWaterQualityPeriod.NoMonitoringProgramForValidUntil", activated.Error.Code);
    }

    // =======================================================================
    // Criteria 6 and 7 — the hold cascade, and what may be booked
    // =======================================================================

    /// <summary>
    /// Criterion 6. Two uses recorded against an Active window both move to
    /// <see cref="WaterUseRecordStatus.Held"/> when the window is held — in the same transaction,
    /// so nothing depends on anyone remembering to go and look.
    /// </summary>
    [Fact]
    public async Task Holding_a_window_flags_every_use_recorded_under_it()
    {
        using var harness = new QcWorksheetTestContext();
        await ArrangeIssuedWaterRound(harness);

        var period = Assert.Single(await PeriodsOf(harness));

        var activated = await harness.WaterQuality.Activate(
            period.Id,
            new ActivateWaterQualityPeriodRequest { RetrospectiveReason = "Cleared." },
            Guid.NewGuid());

        Assert.True(activated.IsSuccess, activated.Error?.Description);

        var batch = await harness.SeedBatchManufacturingRecord("BN-2609-01");
        var recorder = await harness.SeedUser($"prod.{Guid.NewGuid().ToString()[..6]}");

        var firstUse = await harness.WaterQuality.RecordUse(
            new RecordWaterUseRequest
            {
                WaterQualityPeriodId = period.Id,
                UsedAt = DateTime.UtcNow.AddHours(-6),
                BatchManufacturingRecordId = batch.Id
            },
            recorder.Id);

        Assert.True(firstUse.IsSuccess, firstUse.Error?.Description);
        Assert.Equal(WaterUseRecordStatus.Recorded, firstUse.Value.Status);
        Assert.Equal("BN-2609-01", firstUse.Value.BatchNumber);

        var secondUse = await harness.WaterQuality.RecordUse(
            new RecordWaterUseRequest
            {
                WaterQualityPeriodId = period.Id,
                UsedAt = DateTime.UtcNow.AddHours(-2)
            },
            recorder.Id);

        Assert.True(secondUse.IsSuccess, secondUse.Error?.Description);

        var holder = await harness.SeedUser($"qa.{Guid.NewGuid().ToString()[..6]}");

        var held = await harness.WaterQuality.Hold(
            period.Id,
            new HoldWaterQualityPeriodRequest
            {
                HoldReason = "Retest of the same loop exceeded the action limit."
            },
            holder.Id);

        Assert.True(held.IsSuccess, held.Error?.Description);
        Assert.Equal(WaterQualityPeriodStatus.Held, held.Value.Status);
        Assert.Equal(holder.Id, held.Value.HeldBy.Id);
        Assert.NotNull(held.Value.HeldAt);
        Assert.Contains("action limit", held.Value.HoldReason);

        // Both uses flagged, and both still present — a flagged use is never removed, since it is
        // exactly what a Quality Impact Assessment needs to see.
        Assert.Equal(2, held.Value.UseRecordCount);
        Assert.Equal(2, held.Value.HeldUseRecordCount);
        Assert.Equal(2, held.Value.UseRecords.Count);
        Assert.All(held.Value.UseRecords, record =>
            Assert.Equal(WaterUseRecordStatus.Held, record.Status));

        // The activation reason survives the hold rather than being overwritten by it.
        Assert.Equal("Cleared.", held.Value.RetrospectiveReason);

        var stored = await harness.Db.QcWaterUseRecords.AsNoTracking().ToListAsync();
        Assert.Equal(2, stored.Count);
        Assert.All(stored, record => Assert.Equal(WaterUseRecordStatus.Held, record.Status));
    }

    /// <summary>A hold takes a reason, and only an Active window can be held.</summary>
    [Fact]
    public async Task Hold_requires_a_reason_and_an_active_window()
    {
        using var harness = new QcWorksheetTestContext();
        await ArrangeIssuedWaterRound(harness);

        var period = Assert.Single(await PeriodsOf(harness));

        var pending = await harness.WaterQuality.Hold(
            period.Id, new HoldWaterQualityPeriodRequest { HoldReason = "Too early." }, Guid.NewGuid());

        Assert.False(pending.IsSuccess);
        Assert.Equal("QcWaterQualityPeriod.HoldRequiresActive", pending.Error.Code);

        await harness.WaterQuality.Activate(
            period.Id,
            new ActivateWaterQualityPeriodRequest { RetrospectiveReason = "Cleared." },
            Guid.NewGuid());

        var noReason = await harness.WaterQuality.Hold(
            period.Id, new HoldWaterQualityPeriodRequest(), Guid.NewGuid());

        Assert.False(noReason.IsSuccess);
        Assert.Equal("QcWaterQualityPeriod.HoldReasonRequired", noReason.Error.Code);
    }

    /// <summary>
    /// Criterion 7. A use may only be booked against an Active window — never against
    /// PendingActivation scaffolding, and never against one already withdrawn. Either would
    /// assert coverage that was never granted.
    /// </summary>
    [Fact]
    public async Task Use_cannot_be_recorded_outside_an_active_window()
    {
        using var harness = new QcWorksheetTestContext();
        await ArrangeIssuedWaterRound(harness);

        var period = Assert.Single(await PeriodsOf(harness));
        var recorder = await harness.SeedUser($"prod.{Guid.NewGuid().ToString()[..6]}");

        var againstPending = await harness.WaterQuality.RecordUse(
            new RecordWaterUseRequest { WaterQualityPeriodId = period.Id, UsedAt = DateTime.UtcNow },
            recorder.Id);

        Assert.False(againstPending.IsSuccess);
        Assert.Equal("QcWaterUseRecord.RequiresActivePeriod", againstPending.Error.Code);
        Assert.Contains("PendingActivation", againstPending.Error.Description);

        await harness.WaterQuality.Activate(
            period.Id,
            new ActivateWaterQualityPeriodRequest { RetrospectiveReason = "Cleared." },
            Guid.NewGuid());

        await harness.WaterQuality.Hold(
            period.Id,
            new HoldWaterQualityPeriodRequest { HoldReason = "Adjacent point failed." },
            Guid.NewGuid());

        var againstHeld = await harness.WaterQuality.RecordUse(
            new RecordWaterUseRequest { WaterQualityPeriodId = period.Id, UsedAt = DateTime.UtcNow },
            recorder.Id);

        Assert.False(againstHeld.IsSuccess);
        Assert.Equal("QcWaterUseRecord.RequiresActivePeriod", againstHeld.Error.Code);
        Assert.Contains("Held", againstHeld.Error.Description);

        Assert.Empty(await harness.Db.QcWaterUseRecords.AsNoTracking().ToListAsync());
    }

    /// <summary>
    /// An Active window whose <c>ValidUntil</c> has passed is actively flagged Expired by the
    /// daily scan, rather than left saying Active while covering nothing — the locked rule that a
    /// delayed next round must not silently keep covering production.
    /// <para>
    /// Expiry deliberately does not cascade to the uses booked while the window was valid: those
    /// stayed covered. Only a hold, which is a real quality signal, flags them.
    /// </para>
    /// </summary>
    [Fact]
    public async Task The_daily_scan_expires_an_elapsed_window_without_flagging_its_uses()
    {
        using var harness = new QcWorksheetTestContext();
        await ArrangeIssuedWaterRound(harness);

        var period = Assert.Single(await PeriodsOf(harness));
        var recorder = await harness.SeedUser($"prod.{Guid.NewGuid().ToString()[..6]}");

        var activated = await harness.WaterQuality.Activate(
            period.Id,
            new ActivateWaterQualityPeriodRequest { RetrospectiveReason = "Cleared." },
            Guid.NewGuid());

        Assert.True(activated.IsSuccess, activated.Error?.Description);

        var use = await harness.WaterQuality.RecordUse(
            new RecordWaterUseRequest
            {
                WaterQualityPeriodId = period.Id,
                UsedAt = DateTime.UtcNow
            },
            recorder.Id);

        Assert.True(use.IsSuccess, use.Error?.Description);

        // A scan run after the window's end date, with the next round still not performed.
        await harness.MonitoringScan.RunAsync(activated.Value.ValidUntil!.Value.AddDays(1), null);

        var reloaded = await harness.WaterQuality.GetPeriod(period.Id);

        Assert.Equal(WaterQualityPeriodStatus.Expired, reloaded.Value.Status);
        Assert.Equal(0, reloaded.Value.HeldUseRecordCount);
        Assert.Equal(
            WaterUseRecordStatus.Recorded,
            Assert.Single(reloaded.Value.UseRecords).Status);

        // And nothing new may be booked against it.
        var afterExpiry = await harness.WaterQuality.RecordUse(
            new RecordWaterUseRequest { WaterQualityPeriodId = period.Id, UsedAt = DateTime.UtcNow },
            recorder.Id);

        Assert.False(afterExpiry.IsSuccess);
        Assert.Equal("QcWaterUseRecord.RequiresActivePeriod", afterExpiry.Error.Code);
    }

    /// <summary>
    /// Reissuing a revised certificate for the same round must not scaffold a second window over
    /// the same Subject's results — the scaffolding is idempotent per Subject.
    /// </summary>
    [Fact]
    public async Task Scaffolding_is_idempotent_per_subject()
    {
        using var harness = new QcWorksheetTestContext();
        var round = await ArrangeIssuedWaterRound(harness);

        Assert.Single(await PeriodsOf(harness));

        var repeated = await harness.WaterQualityPeriods.ScaffoldForIssuedCoaAsync(
            round.TestRequestId, harness.Approver.Id);

        Assert.Equal(0, repeated);
        Assert.Single(await PeriodsOf(harness));
    }
}
