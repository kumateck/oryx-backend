using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.OosInvestigations;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// Milestone 4's acceptance criteria, end to end against the real repositories, the real
/// approval engine and genuine password hashing — so these prove the production path rather
/// than a parallel one.
/// </summary>
public class OosCaseAcceptanceTests
{
    /// <summary>
    /// A round whose single worksheet has been started, with a Characteristic bound to its
    /// result field. Almost every criterion begins from here.
    /// </summary>
    private sealed record Scenario(
        QcWorksheetTestContext Harness,
        Guid InstanceId,
        User Analyst,
        Specification Specification,
        WorksheetTemplate Template,
        Guid TestRequestId,
        Guid SubjectId);

    private const string FieldKey = "assay";

    private static async Task<Scenario> Arrange(
        QcWorksheetTestContext harness,
        string acceptanceCriteria = null,
        string alertLimit = null,
        string actionLimit = null,
        Guid? materialBatchId = null,
        Guid? batchManufacturingRecordId = null,
        TestRequestType type = TestRequestType.RoutineEnvironmental,
        params string[] extraFieldKeys)
    {
        var fieldKeys = new[] { FieldKey }.Concat(extraFieldKeys).ToArray();

        var template = await harness.SeedEffectiveTemplate(
            $"WS/{Guid.NewGuid().ToString()[..6]}", WorksheetCategory.Microbial, fieldKeys);

        var appliesTo = type switch
        {
            TestRequestType.RawMaterial => SpecificationAppliesTo.RawMaterial,
            TestRequestType.Product => SpecificationAppliesTo.Product,
            _ => SpecificationAppliesTo.RoutineEnvironmental
        };

        var specification = await harness.SeedEffectiveSpecification(
            appliesTo, (template, SpecificationAnalysisType.Microbial));

        await harness.SeedCharacteristic(
            specification, template, FieldKey, acceptanceCriteria, alertLimit, actionLimit);

        var analyst = await harness.SeedUser($"analyst.{Guid.NewGuid().ToString()[..6]}");

        var created = await harness.TestRequests.CreateTestRequest(
            new CreateTestRequestRequest
            {
                Type = type,
                SpecificationId = specification.Id,
                ScheduleOrigin = TestRequestScheduleOrigin.Scheduled,
                ArNumber = $"ARD-{Guid.NewGuid().ToString()[..6]}",
                Subjects =
                [
                    new CreateTestRequestSubjectRequest
                    {
                        SubjectRef = "BATCH-1",
                        SubjectLabel = "Subject under test",
                        MaterialBatchId = materialBatchId,
                        BatchManufacturingRecordId = batchManufacturingRecordId
                    }
                ]
            },
            Guid.NewGuid());

        Assert.True(created.IsSuccess, created.Error?.Description);

        await harness.TestRequests.RecordSample(
            created.Value.Id, new RecordTestRequestSampleRequest(), Guid.NewGuid());

        var subject = created.Value.Subjects.Single();
        var instanceId = subject.WorksheetInstances.Single().Id;

        await harness.WorksheetInstances.Assign(
            instanceId, new AssignWorksheetInstanceRequest { AssignedToId = analyst.Id }, Guid.NewGuid());

        await harness.WorksheetInstances.Start(instanceId, analyst.Id);

        return new Scenario(
            harness, instanceId, analyst, specification, template, created.Value.Id, subject.Id);
    }

    /// <summary>Enters a value and submits, which is what triggers detection.</summary>
    private static async Task SubmitWith(Scenario scenario, string value, string fieldKey = FieldKey)
    {
        var saved = await scenario.Harness.WorksheetInstances.SaveValues(
            scenario.InstanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues = [new WorksheetFieldValueEntry { FieldKey = fieldKey, Value = value }]
            },
            scenario.Analyst.Id);

        Assert.True(saved.IsSuccess, saved.Error?.Description);

        var submitted = await scenario.Harness.WorksheetInstances.Submit(
            scenario.InstanceId, scenario.Analyst.Id);

        Assert.True(submitted.IsSuccess, submitted.Error?.Description);
    }

    /// <summary>Reviews a worksheet as somebody other than the analyst, satisfying segregation of duties.</summary>
    private static async Task<User> ReviewAsSomeoneElse(QcWorksheetTestContext harness, Guid instanceId)
    {
        var reviewer = harness.Approver;

        var reviewed = await harness.WorksheetInstances.Review(
            instanceId,
            new ReviewWorksheetInstanceRequest
            {
                Approve = true,
                Password = QcWorksheetTestContext.CorrectPassword,
                Comments = "Reviewed."
            },
            reviewer.Id,
            [harness.ApproverRole.Id]);

        Assert.True(reviewed.IsSuccess, reviewed.Error?.Description);
        return reviewer;
    }

    // =======================================================================
    // Criterion 1 — auto-detection on ActionLimit breach
    // =======================================================================

    /// <summary>
    /// Criterion 1 — a value failing its Characteristic's Action limit opens a case
    /// automatically, with Status = Open, and the round cannot reach Released.
    /// </summary>
    [Fact]
    public async Task An_action_limit_breach_opens_a_case_and_blocks_release()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var scenario = await Arrange(harness, actionLimit: "NMT 100 CFU/4Hrs");

        await SubmitWith(scenario, "250");

        var oosCase = await harness.Db.QcOosCases
            .SingleAsync(item => item.WorksheetInstanceId == scenario.InstanceId);

        Assert.Equal(OosCaseStatus.Open, oosCase.Status);
        Assert.Equal(FieldKey, oosCase.FieldKey);
        Assert.Equal("250", oosCase.ObservedValue);
        Assert.Equal("NMT 100 CFU/4Hrs", oosCase.BreachedLimit);
        Assert.NotEqual(default, oosCase.OpenedAt);

        // Opened by a limit breach, not by a person — there is deliberately no "opened by".
        Assert.False(oosCase.Approved);

        // The round is blocked, and stays blocked: nothing in the status derivation can reach
        // Released while a case is open.
        var blocked = await harness.OosCases.IsReleaseBlocked(scenario.TestRequestId);
        Assert.True(blocked.Value);

        var round = await harness.Db.QcTestRequests.SingleAsync(item => item.Id == scenario.TestRequestId);
        Assert.NotEqual(TestRequestStatus.Released, round.Status);
    }

    /// <summary>
    /// Resubmitting after a correction cycle re-judges the values without opening a second case
    /// for the same field — otherwise one worksheet could accumulate a case per attempt.
    /// </summary>
    [Fact]
    public async Task Resubmitting_does_not_open_a_duplicate_case_for_the_same_field()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var scenario = await Arrange(harness, actionLimit: "NMT 100 CFU/4Hrs");
        await SubmitWith(scenario, "250");

        await harness.WorksheetInstances.ReturnForCorrection(
            scenario.InstanceId,
            new ReturnWorksheetForCorrectionRequest { Reason = "Recheck the plate count." },
            harness.Approver.Id);

        await SubmitWith(scenario, "260");

        Assert.Single(await harness.Db.QcOosCases
            .Where(item => item.WorksheetInstanceId == scenario.InstanceId)
            .ToListAsync());
    }

    // =======================================================================
    // Criterion 2 — an Alert breach does not create a case
    // =======================================================================

    /// <summary>
    /// Criterion 2 — a value between the Alert and Action limits flags for trend review only.
    /// No case, no block. This is the locked Alert-vs-Action governance rule.
    /// </summary>
    [Fact]
    public async Task An_alert_breach_creates_no_case_and_blocks_nothing()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var scenario = await Arrange(
            harness, alertLimit: "NMT 80 CFU/4Hrs", actionLimit: "NMT 100 CFU/4Hrs");

        // Over Alert (80), under Action (100).
        await SubmitWith(scenario, "85");

        Assert.Empty(await harness.Db.QcOosCases
            .Where(item => item.WorksheetInstanceId == scenario.InstanceId)
            .ToListAsync());

        var blocked = await harness.OosCases.IsReleaseBlocked(scenario.TestRequestId);
        Assert.False(blocked.Value);
    }

    /// <summary>A fully compliant value creates nothing at all.</summary>
    [Fact]
    public async Task A_compliant_value_creates_no_case()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var scenario = await Arrange(
            harness, alertLimit: "NMT 80 CFU/4Hrs", actionLimit: "NMT 100 CFU/4Hrs");

        await SubmitWith(scenario, "40");

        Assert.Empty(await harness.Db.QcOosCases.ToListAsync());
    }

    // =======================================================================
    // Criterion 3 — quarantine happens at investigation start, not at creation
    // =======================================================================

    /// <summary>
    /// Criterion 3 — the real batch status is untouched immediately after auto-creation, and
    /// only moves to a quarantine state once someone starts the investigation.
    /// </summary>
    [Fact]
    public async Task The_batch_is_quarantined_on_investigation_start_not_on_auto_creation()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var batch = await harness.SeedMaterialBatch("MB-001", BatchStatus.Testing);

        var scenario = await Arrange(
            harness,
            actionLimit: "NMT 100 CFU/4Hrs",
            materialBatchId: batch.Id,
            type: TestRequestType.RawMaterial);

        await SubmitWith(scenario, "250");

        // Auto-creation alone must not touch live, shared state.
        var afterDetection = await harness.Db.MaterialBatches.SingleAsync(item => item.Id == batch.Id);
        Assert.Equal(BatchStatus.Testing, afterDetection.Status);

        var oosCase = await harness.Db.QcOosCases.SingleAsync();
        Assert.Null(oosCase.QuarantinedMaterialBatchId);

        var started = await harness.OosCases.StartInvestigation(oosCase.Id, harness.Approver.Id);
        Assert.True(started.IsSuccess, started.Error?.Description);

        var afterStart = await harness.Db.MaterialBatches.SingleAsync(item => item.Id == batch.Id);
        Assert.Equal(BatchStatus.Quarantine, afterStart.Status);

        var quarantined = await harness.Db.QcOosCases.SingleAsync();
        Assert.Equal(OosCaseStatus.InvestigationInProgress, quarantined.Status);
        Assert.Equal(batch.Id, quarantined.QuarantinedMaterialBatchId);

        // The pre-quarantine status is retained as an audit record on the case's own table —
        // no column was added to MaterialBatches to hold it.
        Assert.Equal(BatchStatus.Testing, quarantined.QuarantinedFromBatchStatus);
    }

    /// <summary>
    /// The Product path, the direct mirror of the material one: a real
    /// <see cref="BatchManufacturingStatus.Quarantine"/>, not an approximation. Production and
    /// Warehouse read this field to decide whether a batch may be used, so it has to say
    /// "quarantined" outright rather than something that merely implies it.
    /// </summary>
    [Fact]
    public async Task A_product_record_is_quarantined_on_investigation_start()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var record = await harness.SeedBatchManufacturingRecord("BMR-001", BatchManufacturingStatus.New);

        var scenario = await Arrange(
            harness,
            actionLimit: "NMT 100 CFU/4Hrs",
            batchManufacturingRecordId: record.Id,
            type: TestRequestType.Product);

        await SubmitWith(scenario, "250");

        var untouched = await harness.Db.BatchManufacturingRecords.SingleAsync(item => item.Id == record.Id);
        Assert.Equal(BatchManufacturingStatus.New, untouched.Status);

        var oosCase = await harness.Db.QcOosCases.SingleAsync();
        await harness.OosCases.StartInvestigation(oosCase.Id, harness.Approver.Id);

        var held = await harness.Db.BatchManufacturingRecords.SingleAsync(item => item.Id == record.Id);
        Assert.Equal(BatchManufacturingStatus.Quarantine, held.Status);

        var withAudit = await harness.Db.QcOosCases.SingleAsync();
        Assert.Equal(BatchManufacturingStatus.New, withAudit.QuarantinedFromBatchManufacturingStatus);
    }

    /// <summary>
    /// The Product disposition, both directions. A favourable close releases to
    /// <see cref="BatchManufacturingStatus.Available"/> — deliberately distinct from
    /// <see cref="BatchManufacturingStatus.Approved"/>, which is the normal QA release path, so
    /// the provenance of the release survives.
    /// </summary>
    [Theory]
    [InlineData(OosDispositionOutcome.Invalidated, BatchManufacturingStatus.Available)]
    [InlineData(OosDispositionOutcome.ConfirmedOOS, BatchManufacturingStatus.Rejected)]
    public async Task A_product_disposition_writes_the_real_batch_status(
        OosDispositionOutcome outcome, BatchManufacturingStatus expected)
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.OosCase);

        var record = await harness.SeedBatchManufacturingRecord("BMR-002", BatchManufacturingStatus.New);

        var scenario = await Arrange(
            harness,
            actionLimit: "NMT 100 CFU/4Hrs",
            batchManufacturingRecordId: record.Id,
            type: TestRequestType.Product);

        await SubmitWith(scenario, "250");
        await ReviewAsSomeoneElse(harness, scenario.InstanceId);

        var oosCase = await harness.Db.QcOosCases.SingleAsync();
        await harness.OosCases.StartInvestigation(oosCase.Id, harness.Approver.Id);
        await harness.OosCases.Escalate(
            oosCase.Id, new EscalateOosCaseRequest { Reason = "No lab error." }, harness.Approver.Id);

        var disposed = await harness.OosCases.RecordDisposition(
            oosCase.Id,
            new OosDispositionRequest
            {
                Outcome = outcome,
                Password = QcWorksheetTestContext.CorrectPassword,
                DispositionComments = $"{outcome}."
            },
            harness.Approver.Id,
            [harness.ApproverRole.Id]);

        Assert.True(disposed.IsSuccess, disposed.Error?.Description);

        var after = await harness.Db.BatchManufacturingRecords.SingleAsync(item => item.Id == record.Id);
        Assert.Equal(expected, after.Status);

        var closed = await harness.Db.QcOosCases.SingleAsync();
        Assert.Equal(OosCaseStatus.Closed, closed.Status);
        Assert.Null(closed.QuarantinedBatchManufacturingRecordId);
    }

    /// <summary>
    /// The two appended enum values must not shift any already-persisted numeric value —
    /// anything storing the number rather than the name would silently change meaning.
    /// </summary>
    [Fact]
    public void Existing_batch_manufacturing_status_values_are_not_renumbered()
    {
        Assert.Equal(0, (int)BatchManufacturingStatus.New);
        Assert.Equal(1, (int)BatchManufacturingStatus.Testing);
        Assert.Equal(2, (int)BatchManufacturingStatus.Approved);
        Assert.Equal(3, (int)BatchManufacturingStatus.Rejected);
        Assert.Equal(4, (int)BatchManufacturingStatus.TestTaken);
        Assert.Equal(5, (int)BatchManufacturingStatus.Checked);

        // Appended, so they occupy previously unused numbers.
        Assert.Equal(6, (int)BatchManufacturingStatus.Quarantine);
        Assert.Equal(7, (int)BatchManufacturingStatus.Available);
    }

    // =======================================================================
    // Criterion 4 — retest policy drives subject creation
    // =======================================================================

    /// <summary>
    /// Criterion 4, FreshResample — a new Subject is created under the same round, with its own
    /// CollectedAt, and the retest runs under it.
    /// </summary>
    [Fact]
    public async Task Fresh_resample_creates_a_new_subject_under_the_same_round()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var scenario = await Arrange(harness, actionLimit: "NMT 100 CFU/4Hrs");
        await harness.SetRetestPolicy(scenario.Specification, QcRetestPolicy.FreshResample);

        await SubmitWith(scenario, "250");

        var oosCase = await harness.Db.QcOosCases.SingleAsync();
        await harness.OosCases.StartInvestigation(oosCase.Id, harness.Approver.Id);

        var collectedAt = DateTime.UtcNow.AddHours(-1);
        var authorized = await harness.OosCases.AuthorizeRetest(
            oosCase.Id,
            new AuthorizeOosRetestRequest { CollectedAt = collectedAt, Reason = "Plate contamination." },
            harness.Approver.Id);

        Assert.True(authorized.IsSuccess, authorized.Error?.Description);
        Assert.Equal(OosCaseStatus.RetestRequested, authorized.Value.Status);

        var subjects = await harness.Db.QcTestRequestSubjects
            .Where(item => item.TestRequestId == scenario.TestRequestId)
            .ToListAsync();

        Assert.Equal(2, subjects.Count);

        var resample = subjects.Single(item => item.Id != scenario.SubjectId);
        Assert.Equal(collectedAt, resample.CollectedAt);
        Assert.Equal("BATCH-1", resample.SubjectRef);

        var retest = await harness.Db.QcWorksheetInstances
            .SingleAsync(item => item.Id == authorized.Value.RetestWorksheetInstanceId.Value);

        Assert.Equal(resample.Id, retest.TestRequestSubjectId);
        Assert.NotEqual(scenario.SubjectId, retest.TestRequestSubjectId);
    }

    /// <summary>Criterion 4, SameSample — the retest runs under the original Subject, and no new one appears.</summary>
    [Fact]
    public async Task Same_sample_reuses_the_original_subject()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var scenario = await Arrange(harness, actionLimit: "NMT 100 CFU/4Hrs");
        await harness.SetRetestPolicy(scenario.Specification, QcRetestPolicy.SameSample);

        await SubmitWith(scenario, "250");

        var oosCase = await harness.Db.QcOosCases.SingleAsync();
        await harness.OosCases.StartInvestigation(oosCase.Id, harness.Approver.Id);

        var authorized = await harness.OosCases.AuthorizeRetest(
            oosCase.Id, new AuthorizeOosRetestRequest(), harness.Approver.Id);

        Assert.True(authorized.IsSuccess, authorized.Error?.Description);

        var subjects = await harness.Db.QcTestRequestSubjects
            .Where(item => item.TestRequestId == scenario.TestRequestId)
            .ToListAsync();

        Assert.Single(subjects);

        var retest = await harness.Db.QcWorksheetInstances
            .SingleAsync(item => item.Id == authorized.Value.RetestWorksheetInstanceId.Value);

        Assert.Equal(scenario.SubjectId, retest.TestRequestSubjectId);
    }

    // =======================================================================
    // Criterion 5 — the original result is never touched
    // =======================================================================

    /// <summary>
    /// Criterion 5 — after a retest is authorized and completed, the original worksheet's field
    /// values are unchanged in every respect, and the retest points back at it.
    /// </summary>
    [Fact]
    public async Task The_original_result_is_never_overwritten_by_a_retest()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.OosCase);

        var scenario = await Arrange(harness, actionLimit: "NMT 100 CFU/4Hrs");
        await SubmitWith(scenario, "250");

        // A byte-for-byte snapshot of the original values before anything else happens.
        var before = await harness.Db.QcWorksheetFieldValues
            .AsNoTracking()
            .Where(value => value.WorksheetInstanceId == scenario.InstanceId)
            .OrderBy(value => value.FieldKey)
            .Select(value => new { value.Id, value.FieldKey, value.Value, value.EnteredById, value.EnteredAt })
            .ToListAsync();

        Assert.NotEmpty(before);

        var oosCase = await harness.Db.QcOosCases.SingleAsync();
        await harness.OosCases.StartInvestigation(oosCase.Id, harness.Approver.Id);

        var authorized = await harness.OosCases.AuthorizeRetest(
            oosCase.Id, new AuthorizeOosRetestRequest(), harness.Approver.Id);

        var retestId = authorized.Value.RetestWorksheetInstanceId.Value;

        // Run the retest through to a reviewed result.
        var retester = await harness.SeedUser("analyst.retest");
        await harness.WorksheetInstances.Assign(
            retestId, new AssignWorksheetInstanceRequest { AssignedToId = retester.Id }, Guid.NewGuid());
        await harness.WorksheetInstances.Start(retestId, retester.Id);
        await harness.WorksheetInstances.SaveValues(
            retestId,
            new SaveWorksheetValuesRequest
            {
                FieldValues = [new WorksheetFieldValueEntry { FieldKey = FieldKey, Value = "42" }]
            },
            retester.Id);
        await harness.WorksheetInstances.Submit(retestId, retester.Id);
        await ReviewAsSomeoneElse(harness, retestId);

        var after = await harness.Db.QcWorksheetFieldValues
            .AsNoTracking()
            .Where(value => value.WorksheetInstanceId == scenario.InstanceId)
            .OrderBy(value => value.FieldKey)
            .Select(value => new { value.Id, value.FieldKey, value.Value, value.EnteredById, value.EnteredAt })
            .ToListAsync();

        Assert.Equal(before, after);

        // The link points back at the original, and the original itself still reads 250.
        var retest = await harness.Db.QcWorksheetInstances.SingleAsync(item => item.Id == retestId);
        Assert.Equal(scenario.InstanceId, retest.RetestOfInstanceId);

        Assert.Equal("250", after.Single(value => value.FieldKey == FieldKey).Value);

        // Hard version pinning: the retest runs the version the original ran.
        var original = await harness.Db.QcWorksheetInstances
            .SingleAsync(item => item.Id == scenario.InstanceId);

        Assert.Equal(original.WorksheetTemplateId, retest.WorksheetTemplateId);
        Assert.Equal(original.WorksheetTemplateVersion, retest.WorksheetTemplateVersion);

        // And a completed retest hands the case to QA automatically, opening the disposition
        // round through the same approval engine.
        var advanced = await harness.Db.QcOosCases.SingleAsync();
        Assert.Equal(OosCaseStatus.PendingQaDisposition, advanced.Status);

        Assert.NotEmpty(await harness.Db.QcApprovals
            .Where(item => item.EntityType == QcApprovalEntityTypes.OosCase
                && item.EntityId == advanced.Id)
            .ToListAsync());
    }

    /// <summary>
    /// With no disposition chain configured, reviewing a retest must not destroy the reviewer's
    /// own signed action. The case still advances to PendingQaDisposition and still blocks
    /// release; it simply cannot be disposed until an administrator defines the stages.
    /// </summary>
    [Fact]
    public async Task Reviewing_a_retest_survives_an_unconfigured_disposition_chain()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var scenario = await Arrange(harness, actionLimit: "NMT 100 CFU/4Hrs");
        await SubmitWith(scenario, "250");

        var oosCase = await harness.Db.QcOosCases.SingleAsync();
        await harness.OosCases.StartInvestigation(oosCase.Id, harness.Approver.Id);

        var authorized = await harness.OosCases.AuthorizeRetest(
            oosCase.Id, new AuthorizeOosRetestRequest(), harness.Approver.Id);

        var retestId = authorized.Value.RetestWorksheetInstanceId.Value;
        var retester = await harness.SeedUser("analyst.retest3");

        await harness.WorksheetInstances.Assign(
            retestId, new AssignWorksheetInstanceRequest { AssignedToId = retester.Id }, Guid.NewGuid());
        await harness.WorksheetInstances.Start(retestId, retester.Id);
        await harness.WorksheetInstances.SaveValues(
            retestId,
            new SaveWorksheetValuesRequest
            {
                FieldValues = [new WorksheetFieldValueEntry { FieldKey = FieldKey, Value = "40" }]
            },
            retester.Id);
        await harness.WorksheetInstances.Submit(retestId, retester.Id);

        // The review itself succeeds — the missing chain belongs to a different entity.
        var reviewed = await harness.WorksheetInstances.Review(
            retestId,
            new ReviewWorksheetInstanceRequest
            {
                Approve = true,
                Password = QcWorksheetTestContext.CorrectPassword,
                Comments = "Retest reviewed."
            },
            harness.Approver.Id,
            [harness.ApproverRole.Id]);

        Assert.True(reviewed.IsSuccess, reviewed.Error?.Description);
        Assert.Equal(WorksheetInstanceStatus.Reviewed, reviewed.Value.Status);

        // The case is awaiting QA and still blocking, but nothing was auto-approved.
        var advanced = await harness.Db.QcOosCases.SingleAsync();
        Assert.Equal(OosCaseStatus.PendingQaDisposition, advanced.Status);
        Assert.False(advanced.Approved);

        var blocked = await harness.OosCases.IsReleaseBlocked(scenario.TestRequestId);
        Assert.True(blocked.Value);
    }

    // =======================================================================
    // Criterion 6 — the readiness check blocks premature disposition
    // =======================================================================

    /// <summary>
    /// Criterion 6 — with another worksheet for the same sample still in progress, disposition
    /// is refused; once that worksheet is reviewed, it succeeds.
    /// </summary>
    [Fact]
    public async Task Disposition_is_blocked_until_every_worksheet_for_the_sample_is_reviewed()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.OosCase);

        // Two worksheet tracks on one Specification, so the subject carries two worksheets.
        var chemical = await harness.SeedEffectiveTemplate("WS/CHEM", WorksheetCategory.Chemical, FieldKey);
        var microbial = await harness.SeedEffectiveTemplate("WS/MICRO", WorksheetCategory.Microbial, "viables");

        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RoutineEnvironmental,
            (chemical, SpecificationAnalysisType.Chemical),
            (microbial, SpecificationAnalysisType.Microbial));

        await harness.SeedCharacteristic(specification, chemical, FieldKey, actionLimit: "NMT 100 CFU");

        var analyst = await harness.SeedUser("analyst.two-track");

        var created = await harness.TestRequests.CreateTestRequest(
            new CreateTestRequestRequest
            {
                Type = TestRequestType.RoutineEnvironmental,
                SpecificationId = specification.Id,
                ScheduleOrigin = TestRequestScheduleOrigin.Scheduled,
                ArNumber = "ARD-TWO",
                Subjects = [new CreateTestRequestSubjectRequest { SubjectRef = "SF-1" }]
            },
            Guid.NewGuid());

        await harness.TestRequests.RecordSample(
            created.Value.Id, new RecordTestRequestSampleRequest(), Guid.NewGuid());

        var instances = created.Value.Subjects.Single().WorksheetInstances;
        var chemicalId = instances.Single(item => item.WorksheetTemplateId == chemical.Id).Id;
        var microbialId = instances.Single(item => item.WorksheetTemplateId == microbial.Id).Id;

        // Fail the chemical track, leaving the microbial one untouched and incomplete.
        await harness.WorksheetInstances.Assign(
            chemicalId, new AssignWorksheetInstanceRequest { AssignedToId = analyst.Id }, Guid.NewGuid());
        await harness.WorksheetInstances.Start(chemicalId, analyst.Id);
        await harness.WorksheetInstances.SaveValues(
            chemicalId,
            new SaveWorksheetValuesRequest
            {
                FieldValues = [new WorksheetFieldValueEntry { FieldKey = FieldKey, Value = "250" }]
            },
            analyst.Id);
        await harness.WorksheetInstances.Submit(chemicalId, analyst.Id);
        await ReviewAsSomeoneElse(harness, chemicalId);

        var oosCase = await harness.Db.QcOosCases.SingleAsync();
        await harness.OosCases.StartInvestigation(oosCase.Id, harness.Approver.Id);
        await harness.OosCases.Escalate(
            oosCase.Id, new EscalateOosCaseRequest { Reason = "No lab error found." }, harness.Approver.Id);

        var premature = await harness.OosCases.RecordDisposition(
            oosCase.Id,
            new OosDispositionRequest
            {
                Outcome = OosDispositionOutcome.ConfirmedOOS,
                Password = QcWorksheetTestContext.CorrectPassword,
                DispositionComments = "Confirmed."
            },
            harness.Approver.Id,
            [harness.ApproverRole.Id]);

        Assert.False(premature.IsSuccess);
        Assert.Equal("QcOosCase.DispositionBlockedByIncompleteWork", premature.Error.Code);

        // Nothing was decided, and nothing was signed.
        var stillPending = await harness.Db.QcOosCases.SingleAsync();
        Assert.Equal(OosCaseStatus.PendingQaDisposition, stillPending.Status);
        Assert.Null(stillPending.DispositionOutcome);

        // Complete the other worksheet, then the same call succeeds.
        await harness.WorksheetInstances.Assign(
            microbialId, new AssignWorksheetInstanceRequest { AssignedToId = analyst.Id }, Guid.NewGuid());
        await harness.WorksheetInstances.Start(microbialId, analyst.Id);
        await harness.WorksheetInstances.SaveValues(
            microbialId,
            new SaveWorksheetValuesRequest
            {
                FieldValues = [new WorksheetFieldValueEntry { FieldKey = "viables", Value = "10" }]
            },
            analyst.Id);
        await harness.WorksheetInstances.Submit(microbialId, analyst.Id);
        await ReviewAsSomeoneElse(harness, microbialId);

        var disposed = await harness.OosCases.RecordDisposition(
            oosCase.Id,
            new OosDispositionRequest
            {
                Outcome = OosDispositionOutcome.ConfirmedOOS,
                Password = QcWorksheetTestContext.CorrectPassword,
                DispositionComments = "Confirmed."
            },
            harness.Approver.Id,
            [harness.ApproverRole.Id]);

        Assert.True(disposed.IsSuccess, disposed.Error?.Description);
        Assert.Equal(OosCaseStatus.Closed, disposed.Value.Status);
    }

    // =======================================================================
    // Criterion 7 — the disposition outcome drives the real batch status
    // =======================================================================

    /// <summary>
    /// Criterion 7, ConfirmedOOS — the real MaterialBatch is Rejected and DateRejected is
    /// stamped, exactly as the live OosInvestigation already does.
    /// </summary>
    [Fact]
    public async Task Confirmed_oos_rejects_the_real_batch_and_stamps_the_rejection_date()
    {
        using var harness = new QcWorksheetTestContext();
        var batch = await DisposeScenario(harness, OosDispositionOutcome.ConfirmedOOS);

        Assert.Equal(BatchStatus.Rejected, batch.Status);
        Assert.NotNull(batch.DateRejected);
    }

    /// <summary>Criterion 7, RetestAccepted — the real batch is released back to Available.</summary>
    [Fact]
    public async Task Retest_accepted_releases_the_real_batch()
    {
        using var harness = new QcWorksheetTestContext();
        var batch = await DisposeScenario(harness, OosDispositionOutcome.RetestAccepted, withRetest: true);

        Assert.Equal(BatchStatus.Available, batch.Status);
        Assert.Null(batch.DateRejected);
    }

    /// <summary>Invalidated releases the batch too: the original result was void, not failing.</summary>
    [Fact]
    public async Task Invalidated_releases_the_real_batch()
    {
        using var harness = new QcWorksheetTestContext();
        var batch = await DisposeScenario(harness, OosDispositionOutcome.Invalidated);

        Assert.Equal(BatchStatus.Available, batch.Status);
    }

    /// <summary>Accepting a retest requires there to be one — this is not a free release path.</summary>
    [Fact]
    public async Task Retest_accepted_is_refused_when_no_retest_was_authorized()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.OosCase);

        var oosCase = await EscalatedCase(harness);

        var result = await harness.OosCases.RecordDisposition(
            oosCase.Id,
            new OosDispositionRequest
            {
                Outcome = OosDispositionOutcome.RetestAccepted,
                Password = QcWorksheetTestContext.CorrectPassword
            },
            harness.Approver.Id,
            [harness.ApproverRole.Id]);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcOosCase.RetestAcceptedRequiresRetest", result.Error.Code);
    }

    // =======================================================================
    // Criterion 8 — e-signature through the centralized table
    // =======================================================================

    /// <summary>
    /// Criterion 8 — every disposition writes a QcApproval row with EntityType "OosCase" and
    /// ReauthConfirmedAt set, queryable from the same approvals screen as every other QC
    /// approval. There is no OOS-only signature mechanism.
    /// </summary>
    [Fact]
    public async Task A_disposition_records_a_reauthenticated_signature_in_the_shared_table()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.OosCase);

        var oosCase = await EscalatedCase(harness);

        var disposed = await harness.OosCases.RecordDisposition(
            oosCase.Id,
            new OosDispositionRequest
            {
                Outcome = OosDispositionOutcome.ConfirmedOOS,
                Password = QcWorksheetTestContext.CorrectPassword,
                DispositionComments = "Confirmed out of specification."
            },
            harness.Approver.Id,
            [harness.ApproverRole.Id]);

        Assert.True(disposed.IsSuccess, disposed.Error?.Description);

        var approval = await harness.Db.QcApprovals
            .SingleAsync(item => item.EntityType == QcApprovalEntityTypes.OosCase
                && item.EntityId == oosCase.Id);

        Assert.Equal(ApprovalStatus.Approved, approval.Status);
        Assert.NotNull(approval.ReauthConfirmedAt);
        Assert.Equal(harness.Approver.Id, approval.ApprovedById);

        // Queryable from the same centralized approvals screen the other QC entities use.
        var trail = await harness.Approvals.GetApprovalsForEntity(
            QcApprovalEntityTypes.OosCase, oosCase.Id);

        Assert.True(trail.IsSuccess);
        Assert.Single(trail.Value);
    }

    /// <summary>
    /// The ambient session is deliberately not enough. A wrong password takes no signature, and
    /// the case must be left exactly as it was — not half-disposed.
    /// </summary>
    [Fact]
    public async Task A_disposition_without_valid_reauthentication_changes_nothing()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.OosCase);

        var oosCase = await EscalatedCase(harness);

        var refused = await harness.OosCases.RecordDisposition(
            oosCase.Id,
            new OosDispositionRequest
            {
                Outcome = OosDispositionOutcome.ConfirmedOOS,
                Password = "not-the-right-password"
            },
            harness.Approver.Id,
            [harness.ApproverRole.Id]);

        Assert.False(refused.IsSuccess);

        var untouched = await harness.Db.QcOosCases.SingleAsync(item => item.Id == oosCase.Id);
        Assert.Equal(OosCaseStatus.PendingQaDisposition, untouched.Status);
        Assert.Null(untouched.DispositionOutcome);
        Assert.Null(untouched.DispositionAt);
        Assert.False(untouched.Approved);

        Assert.Empty(await harness.Db.QcApprovals
            .Where(item => item.EntityType == QcApprovalEntityTypes.OosCase
                && item.Status == ApprovalStatus.Approved)
            .ToListAsync());
    }

    /// <summary>
    /// QC's opt-out of the approval engine's silent auto-approval fallback holds here too: with
    /// no configured disposition chain, a case cannot be escalated into a queue nobody owns.
    /// </summary>
    [Fact]
    public async Task A_case_cannot_be_escalated_with_no_disposition_chain_configured()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var scenario = await Arrange(harness, actionLimit: "NMT 100 CFU/4Hrs");
        await SubmitWith(scenario, "250");

        var oosCase = await harness.Db.QcOosCases.SingleAsync();
        await harness.OosCases.StartInvestigation(oosCase.Id, harness.Approver.Id);

        var escalated = await harness.OosCases.Escalate(
            oosCase.Id, new EscalateOosCaseRequest(), harness.Approver.Id);

        Assert.False(escalated.IsSuccess);
        Assert.Equal("QcWorksheet.NoApprovalWorkflowConfigured", escalated.Error.Code);
    }

    // =======================================================================
    // Criterion 9 — coexistence with the live OosInvestigation
    // =======================================================================

    /// <summary>
    /// Criterion 9 — an unrelated live OosInvestigation record is completely unaffected by the
    /// new module: its own table, its own status enum, its own workflow, all untouched.
    /// </summary>
    [Fact]
    public async Task The_live_oos_investigation_path_is_unaffected()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var legacyBatch = await harness.SeedMaterialBatch("LEGACY-1", BatchStatus.Quarantine);

        var legacy = new OosInvestigation
        {
            Id = Guid.NewGuid(),
            MaterialBatchId = legacyBatch.Id,
            BatchNumber = "LEGACY-1",
            ProductOrMaterialName = "Legacy material",
            Status = OosInvestigationStatus.Initiated,
            CreatedAt = DateTime.UtcNow
        };

        harness.Db.OosInvestigations.Add(legacy);
        await harness.Db.SaveChangesAsync();

        // Run the whole new workflow against a different batch entirely.
        var batch = await harness.SeedMaterialBatch("NEW-1", BatchStatus.Testing);
        var scenario = await Arrange(
            harness,
            actionLimit: "NMT 100 CFU/4Hrs",
            materialBatchId: batch.Id,
            type: TestRequestType.RawMaterial);

        await SubmitWith(scenario, "250");

        var oosCase = await harness.Db.QcOosCases.SingleAsync();
        await harness.OosCases.StartInvestigation(oosCase.Id, harness.Approver.Id);

        // The legacy record and its batch are exactly as they were.
        var legacyAfter = await harness.Db.OosInvestigations.SingleAsync(item => item.Id == legacy.Id);
        Assert.Equal(OosInvestigationStatus.Initiated, legacyAfter.Status);
        Assert.Equal(legacyBatch.Id, legacyAfter.MaterialBatchId);
        Assert.Null(legacyAfter.QaReviewedAt);

        var legacyBatchAfter = await harness.Db.MaterialBatches.SingleAsync(item => item.Id == legacyBatch.Id);
        Assert.Equal(BatchStatus.Quarantine, legacyBatchAfter.Status);

        // The two live in separate tables, and neither knows about the other.
        Assert.Single(await harness.Db.OosInvestigations.ToListAsync());
        Assert.Single(await harness.Db.QcOosCases.ToListAsync());
    }

    // =======================================================================
    // Reviewer queue flag
    // =======================================================================

    /// <summary>
    /// A worksheet whose submission triggered a case is flagged in the reviewer queue with a
    /// link into it, so a reviewer cannot sign off a result without seeing it is under formal
    /// investigation.
    /// </summary>
    [Fact]
    public async Task The_reviewer_queue_flags_a_worksheet_with_an_open_case()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var scenario = await Arrange(harness, actionLimit: "NMT 100 CFU/4Hrs");
        await SubmitWith(scenario, "250");

        var queue = await harness.WorksheetInstances.GetReviewQueue(null);
        var card = queue.Value.Items.Single(item => item.Id == scenario.InstanceId);

        Assert.True(card.HasOpenOosCase);
        Assert.NotNull(card.OosCaseId);

        var oosCase = await harness.Db.QcOosCases.SingleAsync();
        Assert.Equal(oosCase.Id, card.OosCaseId);
    }

    // =======================================================================
    // Shared arrangement helpers
    // =======================================================================

    /// <summary>A case escalated to QA and awaiting disposition, on a round with nothing outstanding.</summary>
    private static async Task<OosCase> EscalatedCase(
        QcWorksheetTestContext harness, Guid? materialBatchId = null)
    {
        var scenario = await Arrange(
            harness,
            actionLimit: "NMT 100 CFU/4Hrs",
            materialBatchId: materialBatchId,
            type: materialBatchId.HasValue
                ? TestRequestType.RawMaterial
                : TestRequestType.RoutineEnvironmental);

        await SubmitWith(scenario, "250");
        await ReviewAsSomeoneElse(harness, scenario.InstanceId);

        var oosCase = await harness.Db.QcOosCases.SingleAsync();
        await harness.OosCases.StartInvestigation(oosCase.Id, harness.Approver.Id);

        var escalated = await harness.OosCases.Escalate(
            oosCase.Id, new EscalateOosCaseRequest { Reason = "No lab error found." }, harness.Approver.Id);

        Assert.True(escalated.IsSuccess, escalated.Error?.Description);

        return await harness.Db.QcOosCases.SingleAsync();
    }

    /// <summary>
    /// Runs a case all the way to a signed disposition against a real material batch, and
    /// returns that batch as the database now holds it.
    /// </summary>
    // =======================================================================
    // Looking an OosCase up by the worksheet it was opened against
    // =======================================================================

    /// <summary>
    /// The reviewer queue's question — "is this worksheet under an OOS case, and what came of
    /// it" — answered by the call that already loads the worksheet for review.
    /// <para>
    /// Before this there was no way to ask it without already knowing the case's own id, which
    /// pushed the frontend into re-deriving OOS status by parsing Specification
    /// acceptance-criteria text client-side. That derivation can disagree with the
    /// <c>LimitEvaluator</c> grammar that actually decides whether a case opens, and a reviewer
    /// disagreeing with the system of record about whether a result is out of specification is
    /// the failure this closes.
    /// </para>
    /// </summary>
    [Fact]
    public async Task A_worksheet_with_an_open_case_reports_it_on_its_own_detail()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var scenario = await Arrange(harness, actionLimit: "NMT 100 CFU/4Hrs");
        await SubmitWith(scenario, "250");

        var detail = await harness.WorksheetInstances.GetWorksheetInstance(scenario.InstanceId);
        Assert.True(detail.IsSuccess, detail.Error?.Description);

        var reported = Assert.Single(detail.Value.OosCases);

        // The same case the OOS module holds — not a second, re-derived opinion about it.
        var stored = await harness.Db.QcOosCases
            .SingleAsync(item => item.WorksheetInstanceId == scenario.InstanceId);

        Assert.Equal(stored.Id, reported.Id);
        Assert.Equal(FieldKey, reported.FieldKey);
        Assert.Equal(OosCaseStatus.Open, reported.Status);
        Assert.Equal("250", reported.ObservedValue);
        Assert.Equal("NMT 100 CFU/4Hrs", reported.BreachedLimit);
        Assert.NotEqual(default, reported.OpenedAt);

        // Still open, so still holding its round — and no outcome yet.
        Assert.True(reported.BlocksRelease);
        Assert.Null(reported.DispositionOutcome);
        Assert.Null(reported.RetestWorksheetInstanceId);
    }

    /// <summary>
    /// A compliant result reports an empty list. That is the real answer to "is this out of
    /// specification", not the absence of one — the reviewer queue can rely on it rather than
    /// falling back to parsing criteria text.
    /// </summary>
    [Fact]
    public async Task A_worksheet_with_no_case_reports_none()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var scenario = await Arrange(harness, actionLimit: "NMT 100 CFU/4Hrs");
        await SubmitWith(scenario, "40");

        var detail = await harness.WorksheetInstances.GetWorksheetInstance(scenario.InstanceId);

        Assert.True(detail.IsSuccess, detail.Error?.Description);
        Assert.NotNull(detail.Value.OosCases);
        Assert.Empty(detail.Value.OosCases);
    }

    /// <summary>
    /// Once QA has signed a disposition, the outcome travels with the worksheet: a reviewer
    /// opening a closed case's worksheet sees which of the three findings was reached, and that
    /// the case is no longer holding the round.
    /// </summary>
    [Fact]
    public async Task A_worksheet_with_a_disposed_case_reports_the_outcome()
    {
        using var harness = new QcWorksheetTestContext();
        await DisposeScenario(harness, OosDispositionOutcome.Invalidated);

        var closed = await harness.Db.QcOosCases.SingleAsync();

        var detail = await harness.WorksheetInstances.GetWorksheetInstance(closed.WorksheetInstanceId);
        Assert.True(detail.IsSuccess, detail.Error?.Description);

        var reported = Assert.Single(detail.Value.OosCases);

        Assert.Equal(closed.Id, reported.Id);
        Assert.Equal(OosCaseStatus.Closed, reported.Status);
        Assert.Equal(OosDispositionOutcome.Invalidated, reported.DispositionOutcome);

        // Closed, so it no longer blocks the round.
        Assert.False(reported.BlocksRelease);
    }

    /// <summary>
    /// Granularity is per-FieldKey, so one worksheet can be under several cases at once. The
    /// list reports each, which is what lets a reviewer see <i>which</i> test failed rather than
    /// just that something did.
    /// </summary>
    [Fact]
    public async Task A_worksheet_reports_one_entry_per_failing_field()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        const string secondField = "bioburden";
        var scenario = await Arrange(harness, actionLimit: "NMT 100 CFU/4Hrs", extraFieldKeys: secondField);

        await harness.SeedCharacteristic(
            scenario.Specification, scenario.Template, secondField, null, null, "NMT 10 CFU/g");

        await harness.WorksheetInstances.SaveValues(
            scenario.InstanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues =
                [
                    new WorksheetFieldValueEntry { FieldKey = FieldKey, Value = "250" },
                    new WorksheetFieldValueEntry { FieldKey = secondField, Value = "75" }
                ]
            },
            scenario.Analyst.Id);

        var submitted = await harness.WorksheetInstances.Submit(scenario.InstanceId, scenario.Analyst.Id);
        Assert.True(submitted.IsSuccess, submitted.Error?.Description);

        var detail = await harness.WorksheetInstances.GetWorksheetInstance(scenario.InstanceId);

        Assert.Equal(2, detail.Value.OosCases.Count);
        Assert.Contains(detail.Value.OosCases, item => item.FieldKey == FieldKey);
        Assert.Contains(detail.Value.OosCases, item => item.FieldKey == secondField);
        Assert.All(detail.Value.OosCases, item => Assert.True(item.BlocksRelease));
    }

    // =======================================================================
    // Round-level release blocking, on the TestRequest detail
    // =======================================================================

    /// <summary>
    /// The test-request screen's question — "is this round held by an OOS case, and which one" —
    /// answered by the call that already loads the round.
    /// <para>
    /// The backend knew the answer (<c>IsReleaseBlocked</c>) but exposed it nowhere over HTTP,
    /// so the frontend had to approximate it. An approximation that disagrees with the gate
    /// actually withholding release is the failure this closes: both now read the same
    /// <c>QcReleaseHold</c> predicate.
    /// </para>
    /// </summary>
    [Fact]
    public async Task A_round_with_an_open_case_reports_it_as_blocking_release()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var scenario = await Arrange(harness, actionLimit: "NMT 100 CFU/4Hrs");
        await SubmitWith(scenario, "250");

        var detail = await harness.TestRequests.GetTestRequest(scenario.TestRequestId);
        Assert.True(detail.IsSuccess, detail.Error?.Description);

        Assert.True(detail.Value.BlocksRelease);

        var reported = Assert.Single(detail.Value.BlockingOosCases);

        // The same case the OOS module holds, and the same worksheet it was opened against —
        // the id is what lets the screen link straight to it.
        var stored = await harness.Db.QcOosCases
            .SingleAsync(item => item.WorksheetInstanceId == scenario.InstanceId);

        Assert.Equal(stored.Id, reported.Id);
        Assert.Equal(scenario.InstanceId, reported.WorksheetInstanceId);
        Assert.Equal(FieldKey, reported.FieldKey);
        Assert.Equal(OosCaseStatus.Open, reported.Status);
        Assert.NotEqual(default, reported.OpenedAt);

        // The flag and the enforcing gate are the same answer, by construction.
        var blocked = await harness.OosCases.IsReleaseBlocked(scenario.TestRequestId);
        Assert.Equal(blocked.Value, detail.Value.BlocksRelease);
    }

    /// <summary>
    /// A round with nothing open reports false and an empty list — the real answer, not merely
    /// the absence of one.
    /// </summary>
    [Fact]
    public async Task A_round_with_no_case_reports_no_block()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var scenario = await Arrange(harness, actionLimit: "NMT 100 CFU/4Hrs");
        await SubmitWith(scenario, "40");

        var detail = await harness.TestRequests.GetTestRequest(scenario.TestRequestId);
        Assert.True(detail.IsSuccess, detail.Error?.Description);

        Assert.False(detail.Value.BlocksRelease);
        Assert.NotNull(detail.Value.BlockingOosCases);
        Assert.Empty(detail.Value.BlockingOosCases);

        var blocked = await harness.OosCases.IsReleaseBlocked(scenario.TestRequestId);
        Assert.Equal(blocked.Value, detail.Value.BlocksRelease);
    }

    /// <summary>
    /// A signed disposition closes the case and releases the hold. The round stops reporting a
    /// block, and the closed case drops out of the list entirely rather than lingering with a
    /// false flag — membership of that list <i>is</i> the blocking state.
    /// </summary>
    [Fact]
    public async Task A_round_whose_only_case_is_closed_reports_no_block()
    {
        using var harness = new QcWorksheetTestContext();
        await DisposeScenario(harness, OosDispositionOutcome.Invalidated);

        var closed = await harness.Db.QcOosCases.SingleAsync();
        Assert.Equal(OosCaseStatus.Closed, closed.Status);

        var testRequestId = await harness.Db.QcWorksheetInstances
            .Where(item => item.Id == closed.WorksheetInstanceId)
            .Select(item => item.TestRequestSubject.TestRequestId)
            .SingleAsync();

        var detail = await harness.TestRequests.GetTestRequest(testRequestId);
        Assert.True(detail.IsSuccess, detail.Error?.Description);

        Assert.False(detail.Value.BlocksRelease);
        Assert.Empty(detail.Value.BlockingOosCases);

        var blocked = await harness.OosCases.IsReleaseBlocked(testRequestId);
        Assert.Equal(blocked.Value, detail.Value.BlocksRelease);
    }

    /// <summary>
    /// Several open cases on one round are all listed, oldest first, so the screen can link to
    /// each rather than reporting an undifferentiated "blocked".
    /// </summary>
    [Fact]
    public async Task A_round_lists_every_blocking_case_oldest_first()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        const string secondField = "bioburden";
        var scenario = await Arrange(harness, actionLimit: "NMT 100 CFU/4Hrs", extraFieldKeys: secondField);

        await harness.SeedCharacteristic(
            scenario.Specification, scenario.Template, secondField, null, null, "NMT 10 CFU/g");

        await harness.WorksheetInstances.SaveValues(
            scenario.InstanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues =
                [
                    new WorksheetFieldValueEntry { FieldKey = FieldKey, Value = "250" },
                    new WorksheetFieldValueEntry { FieldKey = secondField, Value = "75" }
                ]
            },
            scenario.Analyst.Id);

        var submitted = await harness.WorksheetInstances.Submit(scenario.InstanceId, scenario.Analyst.Id);
        Assert.True(submitted.IsSuccess, submitted.Error?.Description);

        var detail = await harness.TestRequests.GetTestRequest(scenario.TestRequestId);
        Assert.True(detail.IsSuccess, detail.Error?.Description);

        Assert.True(detail.Value.BlocksRelease);
        Assert.Equal(2, detail.Value.BlockingOosCases.Count);
        Assert.Contains(detail.Value.BlockingOosCases, item => item.FieldKey == FieldKey);
        Assert.Contains(detail.Value.BlockingOosCases, item => item.FieldKey == secondField);

        // Oldest first, and every entry genuinely blocks — a Closed case is never listed.
        Assert.Equal(
            detail.Value.BlockingOosCases.OrderBy(item => item.OpenedAt).Select(item => item.Id),
            detail.Value.BlockingOosCases.Select(item => item.Id));
        Assert.All(
            detail.Value.BlockingOosCases,
            item => Assert.NotEqual(OosCaseStatus.Closed, item.Status));
    }

    private static async Task<MaterialBatch> DisposeScenario(
        QcWorksheetTestContext harness, OosDispositionOutcome outcome, bool withRetest = false)
    {
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.OosCase);

        var batch = await harness.SeedMaterialBatch("MB-DISPOSE", BatchStatus.Testing);

        var scenario = await Arrange(
            harness,
            actionLimit: "NMT 100 CFU/4Hrs",
            materialBatchId: batch.Id,
            type: TestRequestType.RawMaterial);

        await SubmitWith(scenario, "250");
        await ReviewAsSomeoneElse(harness, scenario.InstanceId);

        var oosCase = await harness.Db.QcOosCases.SingleAsync();
        await harness.OosCases.StartInvestigation(oosCase.Id, harness.Approver.Id);

        if (withRetest)
        {
            await harness.SetRetestPolicy(scenario.Specification, QcRetestPolicy.SameSample);

            var authorized = await harness.OosCases.AuthorizeRetest(
                oosCase.Id, new AuthorizeOosRetestRequest(), harness.Approver.Id);

            Assert.True(authorized.IsSuccess, authorized.Error?.Description);

            var retestId = authorized.Value.RetestWorksheetInstanceId.Value;
            var retester = await harness.SeedUser("analyst.retest2");

            await harness.WorksheetInstances.Assign(
                retestId, new AssignWorksheetInstanceRequest { AssignedToId = retester.Id }, Guid.NewGuid());
            await harness.WorksheetInstances.Start(retestId, retester.Id);
            await harness.WorksheetInstances.SaveValues(
                retestId,
                new SaveWorksheetValuesRequest
                {
                    FieldValues = [new WorksheetFieldValueEntry { FieldKey = FieldKey, Value = "40" }]
                },
                retester.Id);
            await harness.WorksheetInstances.Submit(retestId, retester.Id);
            await ReviewAsSomeoneElse(harness, retestId);
        }
        else
        {
            var escalated = await harness.OosCases.Escalate(
                oosCase.Id, new EscalateOosCaseRequest { Reason = "No lab error." }, harness.Approver.Id);

            Assert.True(escalated.IsSuccess, escalated.Error?.Description);
        }

        var disposed = await harness.OosCases.RecordDisposition(
            oosCase.Id,
            new OosDispositionRequest
            {
                Outcome = outcome,
                Password = QcWorksheetTestContext.CorrectPassword,
                DispositionComments = $"{outcome}."
            },
            harness.Approver.Id,
            [harness.ApproverRole.Id]);

        Assert.True(disposed.IsSuccess, disposed.Error?.Description);
        Assert.Equal(OosCaseStatus.Closed, disposed.Value.Status);

        // The quarantine links are cleared on close: the case no longer holds anything.
        var closed = await harness.Db.QcOosCases.SingleAsync();
        Assert.Null(closed.QuarantinedMaterialBatchId);
        Assert.Equal(outcome, closed.DispositionOutcome);

        return await harness.Db.MaterialBatches.SingleAsync(item => item.Id == batch.Id);
    }
}
