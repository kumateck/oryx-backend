using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// Milestone 6's scheduling acceptance criteria (1, 2 and 3), end to end against the real scan
/// service and the real repositories.
/// <para>
/// No test here creates a <c>TestRequest</c> by hand for the scan to find. Every round in this
/// file exists because a monitoring program came due and the scan raised it, which is the only
/// way a scheduled round ever appears in production.
/// </para>
/// </summary>
public class MonitoringProgramAcceptanceTests
{
    private const string MicrobialField = "tamc";

    /// <summary>A Specification a routine round can actually run against.</summary>
    private static async Task<(Specification Specification, WorksheetTemplate Template)> SeedRoutineSpecification(
        QcWorksheetTestContext harness, SpecificationAppliesTo appliesTo)
    {
        var template = await harness.SeedEffectiveTemplate(
            $"WS/MICRO/{Guid.NewGuid().ToString()[..6]}", WorksheetCategory.Microbial, MicrobialField);

        var specification = await harness.SeedEffectiveSpecification(
            appliesTo, (template, SpecificationAnalysisType.Microbial));

        await harness.SeedCharacteristic(
            specification, template, MicrobialField, "NMT 100 CFU/4Hrs", testName: "TAMC");

        return (specification, template);
    }

    private static Task<List<TestRequest>> RoundsOf(QcWorksheetTestContext harness) =>
        harness.Db.QcTestRequests
            .AsNoTracking()
            .Include(item => item.Subjects)
            .ToListAsync();

    // =======================================================================
    // Criterion 1 — grouping, not one TestRequest per program
    // =======================================================================

    /// <summary>
    /// Criterion 1. Five Active programs sharing a Type and a Specification, all due today,
    /// produce <b>one</b> round with five Subjects — not five rounds.
    /// <para>
    /// This is the correction the milestone exists to make. "One point per program" is about
    /// configuration granularity: each point's frequency, Specification and pause state stay
    /// independently manageable. It never meant one round per point, because a real Water round
    /// covers 15+ points and an Environmental round 60–90 rooms in a single physical sweep.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Five_programs_due_the_same_day_produce_one_round_with_five_subjects()
    {
        using var harness = new QcWorksheetTestContext();
        var (specification, _) = await SeedRoutineSpecification(
            harness, SpecificationAppliesTo.RoutineWater);

        var today = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);
        var group = await harness.SeedSamplingPointGroup("Purified Water Loop");

        foreach (var index in Enumerable.Range(1, 5))
        {
            var point = await harness.SeedSamplingPoint(
                $"WP-{index:00}", SamplingPointType.Water, group.Id);

            await harness.SeedMonitoringProgram(point, specification, today);
        }

        var result = await harness.MonitoringScan.RunAsync(today, null);

        Assert.Equal(5, result.ProgramsDue);
        Assert.Equal(5, result.ProgramsGenerated);
        Assert.Equal(1, result.TestRequestsCreated);
        Assert.Empty(result.Skipped);

        var round = Assert.Single(await RoundsOf(harness));

        Assert.Equal(5, round.Subjects.Count);
        Assert.Equal(TestRequestType.RoutineWater, round.Type);
        Assert.Equal(TestRequestScheduleOrigin.Scheduled, round.ScheduleOrigin);

        // Scheduled rounds carry no unscheduled reason — that is the whole distinction the
        // ScheduleOrigin enum draws, and Milestone 3 rejects a reason on a scheduled round.
        Assert.True(string.IsNullOrWhiteSpace(round.UnscheduledReason));

        // Hard version pinning: the round is on the exact version the programs named.
        Assert.Equal(specification.Id, round.SpecificationId);
        Assert.Equal(specification.Version, round.SpecificationVersion);

        // Every Subject names a real point, not just a code — the master-data link the milestone
        // introduces — and carries the point's own Alert/Action tier.
        Assert.All(round.Subjects, subject =>
        {
            Assert.NotNull(subject.SamplingPointId);
            Assert.Equal(group.Id, subject.SamplingPointGroupId);
        });

        Assert.Equal(
            ["WP-01", "WP-02", "WP-03", "WP-04", "WP-05"],
            round.Subjects.Select(subject => subject.SubjectRef).OrderBy(code => code).ToList());

        // One worksheet per (Subject × the pinned Specification's link): the identical shape
        // Milestone 3 builds by hand, so a generated round is indistinguishable downstream.
        var instances = await harness.Db.QcWorksheetInstances.AsNoTracking().ToListAsync();
        Assert.Equal(5, instances.Count);
        Assert.All(instances, instance =>
            Assert.Equal(specification.WorksheetLinks[0].WorksheetTemplateId, instance.WorksheetTemplateId));
    }

    /// <summary>
    /// The grouping is by Type <i>and</i> Specification, not by due date alone. Water and
    /// Environmental points due the same day are different physical rounds run by different
    /// people, and two Water specifications are two different sets of acceptance criteria.
    /// </summary>
    [Fact]
    public async Task Programs_are_grouped_by_type_and_specification_not_merely_by_date()
    {
        using var harness = new QcWorksheetTestContext();

        var (water, _) = await SeedRoutineSpecification(harness, SpecificationAppliesTo.RoutineWater);
        var (otherWater, _) = await SeedRoutineSpecification(harness, SpecificationAppliesTo.RoutineWater);
        var (environmental, _) = await SeedRoutineSpecification(
            harness, SpecificationAppliesTo.RoutineEnvironmental);

        var today = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);

        var wp1 = await harness.SeedSamplingPoint("WP-01");
        var wp2 = await harness.SeedSamplingPoint("WP-02");
        var wp3 = await harness.SeedSamplingPoint("WP-03");
        var room = await harness.SeedSamplingPoint("RM-01", SamplingPointType.Environmental);

        await harness.SeedMonitoringProgram(wp1, water, today);
        await harness.SeedMonitoringProgram(wp2, water, today);
        await harness.SeedMonitoringProgram(wp3, otherWater, today);
        await harness.SeedMonitoringProgram(room, environmental, today);

        var result = await harness.MonitoringScan.RunAsync(today, null);

        Assert.Equal(4, result.ProgramsDue);
        Assert.Equal(3, result.TestRequestsCreated);

        var rounds = await RoundsOf(harness);

        Assert.Equal(2, Assert.Single(rounds, item => item.SpecificationId == water.Id).Subjects.Count);
        Assert.Single(Assert.Single(rounds, item => item.SpecificationId == otherWater.Id).Subjects);

        var emRound = Assert.Single(rounds, item => item.SpecificationId == environmental.Id);
        Assert.Equal(TestRequestType.RoutineEnvironmental, emRound.Type);
        Assert.Single(emRound.Subjects);
    }

    /// <summary>A Paused program is not due, whatever its due date says.</summary>
    [Fact]
    public async Task Paused_programs_are_never_picked_up()
    {
        using var harness = new QcWorksheetTestContext();
        var (specification, _) = await SeedRoutineSpecification(
            harness, SpecificationAppliesTo.RoutineWater);

        var today = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);
        var point = await harness.SeedSamplingPoint("WP-01");

        var program = await harness.SeedMonitoringProgram(
            point, specification, today.AddDays(-30), status: MonitoringProgramStatus.Paused);

        var result = await harness.MonitoringScan.RunAsync(today, null);

        Assert.Equal(0, result.ProgramsDue);
        Assert.Empty(await RoundsOf(harness));

        // And its due date is untouched: a paused program comes back exactly as overdue as it
        // went in, rather than silently rolled forward.
        var reloaded = await harness.Db.QcMonitoringPrograms.AsNoTracking()
            .SingleAsync(item => item.Id == program.Id);

        Assert.Equal(today.AddDays(-30), reloaded.NextDueDate);
    }

    /// <summary>
    /// LeadTimeDays pulls a round forward so sampling and incubation can start on time — the
    /// program is picked up before its due date, not on it.
    /// </summary>
    [Fact]
    public async Task Lead_time_raises_the_round_before_the_due_date()
    {
        using var harness = new QcWorksheetTestContext();
        var (specification, _) = await SeedRoutineSpecification(
            harness, SpecificationAppliesTo.RoutineWater);

        var today = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);

        var early = await harness.SeedSamplingPoint("WP-01");
        var late = await harness.SeedSamplingPoint("WP-02");

        // Due in three days, with three days of lead time: inside the window.
        await harness.SeedMonitoringProgram(early, specification, today.AddDays(3), leadTimeDays: 3);

        // Due in three days with no lead time: not yet.
        await harness.SeedMonitoringProgram(late, specification, today.AddDays(3));

        var result = await harness.MonitoringScan.RunAsync(today, null);

        Assert.Equal(1, result.ProgramsDue);
        var round = Assert.Single(await RoundsOf(harness));
        Assert.Equal("WP-01", Assert.Single(round.Subjects).SubjectRef);
    }

    // =======================================================================
    // Criterion 2 — NextDueDate advances at generation, not completion
    // =======================================================================

    /// <summary>
    /// Criterion 2. Each included program's due date has already moved on by its own Frequency
    /// the moment the round is created — before any worksheet under it has been touched — and a
    /// second scan the same day creates nothing.
    /// <para>
    /// Advancing at completion instead would tie the schedule to lab turnaround: a round still in
    /// incubation would leave its programs perpetually due, and every subsequent scan would raise
    /// another round for the same occurrence.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Due_dates_advance_at_generation_and_a_second_scan_creates_nothing()
    {
        using var harness = new QcWorksheetTestContext();
        var (specification, _) = await SeedRoutineSpecification(
            harness, SpecificationAppliesTo.RoutineWater);

        var today = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);

        var weeklyPoint = await harness.SeedSamplingPoint("WP-01");
        var monthlyPoint = await harness.SeedSamplingPoint("WP-02");
        var customPoint = await harness.SeedSamplingPoint("WP-03");

        var weekly = await harness.SeedMonitoringProgram(
            weeklyPoint, specification, today, MonitoringFrequency.Weekly);

        var monthly = await harness.SeedMonitoringProgram(
            monthlyPoint, specification, today, MonitoringFrequency.Monthly);

        var custom = await harness.SeedMonitoringProgram(
            customPoint, specification, today, MonitoringFrequency.Custom, customIntervalDays: 10);

        var first = await harness.MonitoringScan.RunAsync(today, null);
        Assert.Equal(1, first.TestRequestsCreated);

        var programs = await harness.Db.QcMonitoringPrograms.AsNoTracking()
            .ToDictionaryAsync(item => item.Id);

        // Advanced by each program's own frequency, from its own previous due date.
        Assert.Equal(today.AddDays(7), programs[weekly.Id].NextDueDate);
        Assert.Equal(today.AddMonths(1), programs[monthly.Id].NextDueDate);
        Assert.Equal(today.AddDays(10), programs[custom.Id].NextDueDate);

        // Nothing under the generated round has been touched — the advance did not wait for it.
        var instances = await harness.Db.QcWorksheetInstances.AsNoTracking().ToListAsync();
        Assert.All(instances, instance =>
            Assert.Equal(WorksheetInstanceStatus.NotStarted, instance.Status));

        var second = await harness.MonitoringScan.RunAsync(today, null);

        Assert.Equal(0, second.ProgramsDue);
        Assert.Equal(0, second.TestRequestsCreated);
        Assert.Single(await RoundsOf(harness));
    }

    /// <summary>
    /// An overdue program catches up to the next future slot in one scan rather than replaying
    /// every occurrence it missed. Thirty un-run daily tests are not thirty rounds that can still
    /// be performed — they are thirty samples nobody took.
    /// </summary>
    [Fact]
    public async Task An_overdue_program_catches_up_to_the_next_future_slot_in_one_round()
    {
        using var harness = new QcWorksheetTestContext();
        var (specification, _) = await SeedRoutineSpecification(
            harness, SpecificationAppliesTo.RoutineWater);

        var today = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);
        var point = await harness.SeedSamplingPoint("WP-01");

        var program = await harness.SeedMonitoringProgram(
            point, specification, today.AddDays(-30), MonitoringFrequency.Daily);

        var result = await harness.MonitoringScan.RunAsync(today, null);

        Assert.Equal(1, result.TestRequestsCreated);

        var reloaded = await harness.Db.QcMonitoringPrograms.AsNoTracking()
            .SingleAsync(item => item.Id == program.Id);

        Assert.Equal(today.AddDays(1), reloaded.NextDueDate);
    }

    /// <summary>
    /// The second guard behind criterion 2: an open round already covering the point suppresses a
    /// new one even if the due date was edited backwards. The program is left visibly Overdue
    /// rather than silently advanced, so the gap shows in the calendar instead of disappearing.
    /// </summary>
    [Fact]
    public async Task An_open_round_for_the_same_point_suppresses_a_duplicate_and_leaves_it_overdue()
    {
        using var harness = new QcWorksheetTestContext();
        var (specification, _) = await SeedRoutineSpecification(
            harness, SpecificationAppliesTo.RoutineWater);

        var today = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);
        var point = await harness.SeedSamplingPoint("WP-01");
        var program = await harness.SeedMonitoringProgram(point, specification, today);

        await harness.MonitoringScan.RunAsync(today, null);

        // Somebody drags the schedule back while the first round is still in the lab.
        var tracked = await harness.Db.QcMonitoringPrograms.SingleAsync(item => item.Id == program.Id);
        tracked.NextDueDate = today;
        await harness.Db.SaveChangesAsync();

        var second = await harness.MonitoringScan.RunAsync(today, null);

        Assert.Equal(1, second.ProgramsDue);
        Assert.Equal(0, second.TestRequestsCreated);

        var skip = Assert.Single(second.Skipped);
        Assert.Equal(program.Id, skip.MonitoringProgramId);
        Assert.Contains("open test request", skip.Reason);

        Assert.Single(await RoundsOf(harness));

        var reloaded = await harness.Db.QcMonitoringPrograms.AsNoTracking()
            .SingleAsync(item => item.Id == program.Id);

        Assert.Equal(today, reloaded.NextDueDate);
    }

    /// <summary>
    /// Hard version pinning has a consequence the scan has to face: a program pinned to a
    /// Specification that has since been superseded does not follow the revision on its own, so
    /// the scan refuses to raise a round against a withdrawn controlled document, leaves the
    /// program overdue, and says exactly why.
    /// <para>
    /// The alternative — walking <c>SupersedesId</c> forward at 02:00 — would be the first forward
    /// resolution anywhere in this module and would let a Specification revision change what a
    /// schedule tests without anybody deciding so.
    /// </para>
    /// </summary>
    [Fact]
    public async Task A_program_pinned_to_a_superseded_specification_is_skipped_with_a_reason()
    {
        using var harness = new QcWorksheetTestContext();
        var (specification, _) = await SeedRoutineSpecification(
            harness, SpecificationAppliesTo.RoutineWater);

        var today = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);
        var point = await harness.SeedSamplingPoint("WP-01");
        var program = await harness.SeedMonitoringProgram(point, specification, today);

        var tracked = await harness.Db.QcSpecifications.SingleAsync(item => item.Id == specification.Id);
        tracked.Status = QcDocumentStatus.Superseded;
        await harness.Db.SaveChangesAsync();

        var result = await harness.MonitoringScan.RunAsync(today, null);

        Assert.Equal(1, result.ProgramsDue);
        Assert.Equal(0, result.TestRequestsCreated);
        Assert.Empty(await RoundsOf(harness));

        var skip = Assert.Single(result.Skipped);
        Assert.Contains("Superseded", skip.Reason);

        var reloaded = await harness.Db.QcMonitoringPrograms.AsNoTracking()
            .SingleAsync(item => item.Id == program.Id);

        Assert.Equal(today, reloaded.NextDueDate);
    }

    // =======================================================================
    // Criterion 3 — SamplingPoint is a picker on TestRequestSubject
    // =======================================================================

    /// <summary>
    /// Criterion 3. A manually added routine Subject names a point from the master table, and the
    /// point is the authority on its own code, name and Alert/Action tier — free text passed
    /// alongside it does not win, which is the whole reason the table exists.
    /// </summary>
    [Fact]
    public async Task A_manually_added_routine_subject_resolves_its_sampling_point_from_master_data()
    {
        using var harness = new QcWorksheetTestContext();
        var (specification, _) = await SeedRoutineSpecification(
            harness, SpecificationAppliesTo.RoutineWater);

        var group = await harness.SeedSamplingPointGroup("Purified Water Loop");
        var point = await harness.SeedSamplingPoint("WP-07", SamplingPointType.Water, group.Id);

        var created = await harness.TestRequests.CreateTestRequest(
            new CreateTestRequestRequest
            {
                Type = TestRequestType.RoutineWater,
                SpecificationId = specification.Id,
                ScheduleOrigin = TestRequestScheduleOrigin.Unscheduled,
                UnscheduledReason = "Investigation resample.",
                ArNumber = "ARD-MANUAL-1",
                Subjects =
                [
                    new CreateTestRequestSubjectRequest
                    {
                        // Typed nonsense alongside the picked point. The point wins.
                        SubjectRef = "wp7 (typo)",
                        SubjectLabel = "typed label",
                        SamplingPointId = point.Id
                    }
                ]
            },
            Guid.NewGuid());

        Assert.True(created.IsSuccess, created.Error?.Description);

        var subject = Assert.Single(created.Value.Subjects);
        Assert.Equal(point.Id, subject.SamplingPointId);
        Assert.Equal("WP-07", subject.SubjectRef);
        Assert.Equal(point.Name, subject.SubjectLabel);
        Assert.Equal(group.Id, subject.SamplingPointGroupId);
    }

    /// <summary>
    /// The same resolution on the incremental add-Subject path Milestone 3 exposes, which is the
    /// one criterion 3 actually names.
    /// </summary>
    [Fact]
    public async Task Adding_a_subject_later_resolves_its_sampling_point_the_same_way()
    {
        using var harness = new QcWorksheetTestContext();
        var (specification, _) = await SeedRoutineSpecification(
            harness, SpecificationAppliesTo.RoutineEnvironmental);

        var group = await harness.SeedSamplingPointGroup("Dispensing Booth");
        var first = await harness.SeedSamplingPoint("RM-01", SamplingPointType.Environmental);
        var added = await harness.SeedSamplingPoint("RM-02", SamplingPointType.Environmental, group.Id);

        var created = await harness.TestRequests.CreateTestRequest(
            new CreateTestRequestRequest
            {
                Type = TestRequestType.RoutineEnvironmental,
                SpecificationId = specification.Id,
                ScheduleOrigin = TestRequestScheduleOrigin.Scheduled,
                ArNumber = "ARD-EM-1",
                Subjects = [new CreateTestRequestSubjectRequest { SamplingPointId = first.Id }]
            },
            Guid.NewGuid());

        Assert.True(created.IsSuccess, created.Error?.Description);

        // SubjectRef was never supplied and is no longer required when a point is named — the
        // picker supplies the code.
        Assert.Equal("RM-01", Assert.Single(created.Value.Subjects).SubjectRef);

        var updated = await harness.TestRequests.AddSubjects(
            created.Value.Id,
            new AddTestRequestSubjectsRequest
            {
                Subjects = [new CreateTestRequestSubjectRequest { SamplingPointId = added.Id }]
            },
            Guid.NewGuid());

        Assert.True(updated.IsSuccess, updated.Error?.Description);

        var subject = Assert.Single(updated.Value.Subjects, item => item.SamplingPointId == added.Id);
        Assert.Equal("RM-02", subject.SubjectRef);
        Assert.Equal(group.Id, subject.SamplingPointGroupId);
    }

    /// <summary>
    /// A point still may not be invented: an unknown id, a point from the wrong discipline, and a
    /// point on a batch round are each refused server-side rather than left to the picker.
    /// </summary>
    [Fact]
    public async Task Sampling_point_selection_is_validated_not_merely_offered()
    {
        using var harness = new QcWorksheetTestContext();
        var (water, _) = await SeedRoutineSpecification(harness, SpecificationAppliesTo.RoutineWater);
        var (material, _) = await SeedRoutineSpecification(harness, SpecificationAppliesTo.RoutineWater);

        var materialTemplate = await harness.SeedEffectiveTemplate(
            $"WS/CHEM/{Guid.NewGuid().ToString()[..6]}", WorksheetCategory.Chemical, "assay");

        var materialSpecification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RawMaterial, (materialTemplate, SpecificationAnalysisType.Chemical));

        var room = await harness.SeedSamplingPoint("RM-01", SamplingPointType.Environmental);
        var waterPoint = await harness.SeedSamplingPoint("WP-01");

        // Unknown point.
        var unknown = await harness.TestRequests.CreateTestRequest(
            new CreateTestRequestRequest
            {
                Type = TestRequestType.RoutineWater,
                SpecificationId = water.Id,
                ScheduleOrigin = TestRequestScheduleOrigin.Scheduled,
                ArNumber = "ARD-1",
                Subjects = [new CreateTestRequestSubjectRequest { SamplingPointId = Guid.NewGuid() }]
            },
            Guid.NewGuid());

        Assert.False(unknown.IsSuccess);
        Assert.Equal("QcSamplingPoint.NotFound", unknown.Error.Code);

        // An Environmental point on a Water round.
        var mismatched = await harness.TestRequests.CreateTestRequest(
            new CreateTestRequestRequest
            {
                Type = TestRequestType.RoutineWater,
                SpecificationId = material.Id,
                ScheduleOrigin = TestRequestScheduleOrigin.Scheduled,
                ArNumber = "ARD-2",
                Subjects = [new CreateTestRequestSubjectRequest { SamplingPointId = room.Id }]
            },
            Guid.NewGuid());

        Assert.False(mismatched.IsSuccess);
        Assert.Equal("QcTestRequest.SamplingPointTypeMismatch", mismatched.Error.Code);

        // A sampling point on a batch round, which has no point concept at all.
        var batch = await harness.TestRequests.CreateTestRequest(
            new CreateTestRequestRequest
            {
                Type = TestRequestType.RawMaterial,
                SpecificationId = materialSpecification.Id,
                ScheduleOrigin = TestRequestScheduleOrigin.Scheduled,
                ArNumber = "ARD-3",
                Subjects =
                [
                    new CreateTestRequestSubjectRequest
                    {
                        SubjectRef = "BN-001",
                        SamplingPointId = waterPoint.Id
                    }
                ]
            },
            Guid.NewGuid());

        Assert.False(batch.IsSuccess);
        Assert.Equal("QcTestRequest.SamplingPointIsRoutineOnly", batch.Error.Code);
    }

    /// <summary>
    /// The link stays optional. Unscheduled routine testing can still name a point that has no
    /// master-data row yet, exactly as it could before this milestone — the picker is the
    /// improvement, not a new barrier to raising a sample nobody has configured.
    /// </summary>
    [Fact]
    public async Task A_routine_subject_may_still_be_raised_without_a_sampling_point()
    {
        using var harness = new QcWorksheetTestContext();
        var (specification, _) = await SeedRoutineSpecification(
            harness, SpecificationAppliesTo.RoutineWater);

        var created = await harness.TestRequests.CreateTestRequest(
            new CreateTestRequestRequest
            {
                Type = TestRequestType.RoutineWater,
                SpecificationId = specification.Id,
                ScheduleOrigin = TestRequestScheduleOrigin.Unscheduled,
                UnscheduledReason = "New tap, not yet configured.",
                ArNumber = "ARD-ADHOC",
                Subjects = [new CreateTestRequestSubjectRequest { SubjectRef = "WP-NEW" }]
            },
            Guid.NewGuid());

        Assert.True(created.IsSuccess, created.Error?.Description);

        var subject = Assert.Single(created.Value.Subjects);
        Assert.Null(subject.SamplingPointId);
        Assert.Equal("WP-NEW", subject.SubjectRef);
    }

    /// <summary>
    /// A Subject with neither a point nor a code is still refused — the rule Milestone 3 already
    /// enforced, unchanged by making SubjectRef optional at the binding layer.
    /// </summary>
    [Fact]
    public async Task A_subject_with_neither_a_point_nor_a_code_is_refused()
    {
        using var harness = new QcWorksheetTestContext();
        var (specification, _) = await SeedRoutineSpecification(
            harness, SpecificationAppliesTo.RoutineWater);

        var created = await harness.TestRequests.CreateTestRequest(
            new CreateTestRequestRequest
            {
                Type = TestRequestType.RoutineWater,
                SpecificationId = specification.Id,
                ScheduleOrigin = TestRequestScheduleOrigin.Scheduled,
                ArNumber = "ARD-EMPTY",
                Subjects = [new CreateTestRequestSubjectRequest()]
            },
            Guid.NewGuid());

        Assert.False(created.IsSuccess);
        Assert.Equal("QcTestRequest.SubjectRefRequired", created.Error.Code);
    }

    // =======================================================================
    // Configuration guards
    // =======================================================================

    /// <summary>
    /// A monitoring program is operational configuration, but it still may only schedule testing
    /// against an Effective Specification for its point's own discipline — the same gate a round
    /// passes at creation. Nothing about "not a controlled document" loosens what it points at.
    /// </summary>
    [Fact]
    public async Task A_program_may_only_schedule_an_effective_specification_of_the_matching_type()
    {
        using var harness = new QcWorksheetTestContext();
        var (water, _) = await SeedRoutineSpecification(harness, SpecificationAppliesTo.RoutineWater);
        var (environmental, _) = await SeedRoutineSpecification(
            harness, SpecificationAppliesTo.RoutineEnvironmental);

        var point = await harness.SeedSamplingPoint("WP-01");
        var today = DateTime.UtcNow.Date;

        var mismatched = await harness.MonitoringPrograms.CreateMonitoringProgram(
            new CreateMonitoringProgramRequest
            {
                SamplingPointId = point.Id,
                SpecificationId = environmental.Id,
                Frequency = MonitoringFrequency.Weekly,
                NextDueDate = today
            },
            Guid.NewGuid());

        Assert.False(mismatched.IsSuccess);
        Assert.Equal("QcMonitoringProgram.SpecificationTypeMismatch", mismatched.Error.Code);

        var draft = await harness.Db.QcSpecifications.SingleAsync(item => item.Id == water.Id);
        draft.Status = QcDocumentStatus.Draft;
        await harness.Db.SaveChangesAsync();

        var notEffective = await harness.MonitoringPrograms.CreateMonitoringProgram(
            new CreateMonitoringProgramRequest
            {
                SamplingPointId = point.Id,
                SpecificationId = water.Id,
                Frequency = MonitoringFrequency.Weekly,
                NextDueDate = today
            },
            Guid.NewGuid());

        Assert.False(notEffective.IsSuccess);
        Assert.Equal("QcMonitoringProgram.SpecificationNotEffective", notEffective.Error.Code);
    }

    /// <summary>
    /// A created program pins the Specification version server-side, and Custom is the only
    /// frequency that may carry an interval.
    /// </summary>
    [Fact]
    public async Task A_created_program_pins_its_specification_version_and_validates_its_interval()
    {
        using var harness = new QcWorksheetTestContext();
        var (specification, _) = await SeedRoutineSpecification(
            harness, SpecificationAppliesTo.RoutineWater);

        var point = await harness.SeedSamplingPoint("WP-01");
        var today = DateTime.UtcNow.Date;

        var missingInterval = await harness.MonitoringPrograms.CreateMonitoringProgram(
            new CreateMonitoringProgramRequest
            {
                SamplingPointId = point.Id,
                SpecificationId = specification.Id,
                Frequency = MonitoringFrequency.Custom,
                NextDueDate = today
            },
            Guid.NewGuid());

        Assert.False(missingInterval.IsSuccess);
        Assert.Equal("QcMonitoringProgram.CustomIntervalRequired", missingInterval.Error.Code);

        var strayInterval = await harness.MonitoringPrograms.CreateMonitoringProgram(
            new CreateMonitoringProgramRequest
            {
                SamplingPointId = point.Id,
                SpecificationId = specification.Id,
                Frequency = MonitoringFrequency.Weekly,
                CustomIntervalDays = 10,
                NextDueDate = today
            },
            Guid.NewGuid());

        Assert.False(strayInterval.IsSuccess);
        Assert.Equal("QcMonitoringProgram.CustomIntervalNotApplicable", strayInterval.Error.Code);

        var created = await harness.MonitoringPrograms.CreateMonitoringProgram(
            new CreateMonitoringProgramRequest
            {
                SamplingPointId = point.Id,
                SpecificationId = specification.Id,
                Frequency = MonitoringFrequency.Weekly,
                NextDueDate = today
            },
            Guid.NewGuid());

        Assert.True(created.IsSuccess, created.Error?.Description);
        Assert.Equal(specification.Version, created.Value.SpecificationVersion);
        Assert.Equal(MonitoringProgramStatus.Active, created.Value.Status);

        // A second identical program would simply raise the same round twice.
        var duplicate = await harness.MonitoringPrograms.CreateMonitoringProgram(
            new CreateMonitoringProgramRequest
            {
                SamplingPointId = point.Id,
                SpecificationId = specification.Id,
                Frequency = MonitoringFrequency.Daily,
                NextDueDate = today
            },
            Guid.NewGuid());

        Assert.False(duplicate.IsSuccess);
        Assert.Equal("QcMonitoringProgram.Duplicate", duplicate.Error.Code);
    }

    /// <summary>
    /// Pause and Resume are the program's only state transitions, and each is refused from the
    /// wrong state rather than silently no-ops.
    /// </summary>
    [Fact]
    public async Task Pause_and_resume_guard_their_states()
    {
        using var harness = new QcWorksheetTestContext();
        var (specification, _) = await SeedRoutineSpecification(
            harness, SpecificationAppliesTo.RoutineWater);

        var point = await harness.SeedSamplingPoint("WP-01");
        var program = await harness.SeedMonitoringProgram(point, specification, DateTime.UtcNow.Date);

        var resumeActive = await harness.MonitoringPrograms.Resume(program.Id, Guid.NewGuid());
        Assert.False(resumeActive.IsSuccess);
        Assert.Equal("QcMonitoringProgram.ResumeRequiresPaused", resumeActive.Error.Code);

        var paused = await harness.MonitoringPrograms.Pause(program.Id, Guid.NewGuid());
        Assert.True(paused.IsSuccess, paused.Error?.Description);
        Assert.Equal(MonitoringProgramStatus.Paused, paused.Value.Status);
        Assert.Equal(MonitoringDueBucket.Paused, paused.Value.DueBucket);

        var pauseAgain = await harness.MonitoringPrograms.Pause(program.Id, Guid.NewGuid());
        Assert.False(pauseAgain.IsSuccess);
        Assert.Equal("QcMonitoringProgram.PauseRequiresActive", pauseAgain.Error.Code);

        var resumed = await harness.MonitoringPrograms.Resume(program.Id, Guid.NewGuid());
        Assert.True(resumed.IsSuccess, resumed.Error?.Description);
        Assert.Equal(MonitoringProgramStatus.Active, resumed.Value.Status);
    }

    /// <summary>
    /// Sampling point codes are unique among live points — the single failure mode the master
    /// table exists to prevent — and a point still scheduled cannot be deleted out from under
    /// its program.
    /// </summary>
    [Fact]
    public async Task Sampling_point_codes_are_unique_and_a_scheduled_point_cannot_be_deleted()
    {
        using var harness = new QcWorksheetTestContext();
        var (specification, _) = await SeedRoutineSpecification(
            harness, SpecificationAppliesTo.RoutineWater);

        var created = await harness.SamplingPoints.CreateSamplingPoint(
            new CreateSamplingPointRequest
            {
                Code = "WP-01",
                Name = "Purified water outlet 1",
                Type = SamplingPointType.Water
            },
            Guid.NewGuid());

        Assert.True(created.IsSuccess, created.Error?.Description);

        var duplicate = await harness.SamplingPoints.CreateSamplingPoint(
            new CreateSamplingPointRequest
            {
                Code = "wp-01",
                Name = "Same tap, typed again",
                Type = SamplingPointType.Water
            },
            Guid.NewGuid());

        Assert.False(duplicate.IsSuccess);
        Assert.Equal("QcSamplingPoint.DuplicateCode", duplicate.Error.Code);

        var point = await harness.Db.QcSamplingPoints.SingleAsync(item => item.Id == created.Value.Id);
        await harness.SeedMonitoringProgram(point, specification, DateTime.UtcNow.Date);

        var deleted = await harness.SamplingPoints.DeleteSamplingPoint(point.Id, Guid.NewGuid());

        Assert.False(deleted.IsSuccess);
        Assert.Equal("QcSamplingPoint.InUse", deleted.Error.Code);
    }
}
