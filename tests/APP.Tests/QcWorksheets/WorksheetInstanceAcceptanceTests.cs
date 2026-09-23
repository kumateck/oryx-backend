using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// Milestone 3 acceptance criteria 2-7: assignment enforcement, audited reassignment, the
/// non-editable header block, the hard instrument and reagent gates, and ReferencedResult
/// resolving only against a reviewed source. Criterion 1 lives in
/// <see cref="TestRequestAcceptanceTests"/> and criterion 8 in
/// <see cref="QcWorksheetCoexistenceTests"/>, matching where Milestones 1 and 2 put theirs.
/// </summary>
public class WorksheetInstanceAcceptanceTests
{
    private static WorksheetField Field(
        string key, WorksheetFieldType type, WorksheetFieldMode mode = WorksheetFieldMode.Entry,
        int order = 1) => new()
    {
        FieldKey = key,
        Label = key,
        Type = type,
        Mode = mode,
        Order = order
    };

    /// <summary>
    /// A round with one subject and one started worksheet, which is the state most of these
    /// criteria begin from.
    /// </summary>
    private static async Task<(Guid InstanceId, User Analyst)> StartedWorksheet(
        QcWorksheetTestContext harness, WorksheetTemplate template)
    {
        var analyst = await harness.SeedUser("analyst.a");

        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RoutineEnvironmental, (template, SpecificationAnalysisType.Microbial));

        var created = await harness.TestRequests.CreateTestRequest(
            new CreateTestRequestRequest
            {
                Type = TestRequestType.RoutineEnvironmental,
                SpecificationId = specification.Id,
                ScheduleOrigin = TestRequestScheduleOrigin.Scheduled,
                ArNumber = "ARD-2609124",
                Subjects = [new CreateTestRequestSubjectRequest { SubjectRef = "SF-91", SubjectLabel = "Deblistering-2" }]
            },
            Guid.NewGuid());

        // The sample is recorded first, as it is in the real flow: a round only leaves Draft
        // when collection happens, and its status is derived from its worksheets after that.
        await harness.TestRequests.RecordSample(
            created.Value.Id, new RecordTestRequestSampleRequest(), Guid.NewGuid());

        var instanceId = created.Value.Subjects.Single().WorksheetInstances.Single().Id;

        await harness.WorksheetInstances.Assign(
            instanceId, new AssignWorksheetInstanceRequest { AssignedToId = analyst.Id }, Guid.NewGuid());

        await harness.WorksheetInstances.Start(instanceId, analyst.Id);

        return (instanceId, analyst);
    }

    // -----------------------------------------------------------------------
    // Criterion 2 — assignment enforcement
    // -----------------------------------------------------------------------

    /// <summary>
    /// Criterion 2 — a second analyst who holds the start permission still cannot start someone
    /// else's worksheet. The permission layer is not the last word; the assignment is.
    /// </summary>
    [Fact]
    public async Task Another_analyst_cannot_start_a_worksheet_assigned_to_someone_else()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("WS/EM/1", WorksheetCategory.Microbial, "viables");
        var analystA = await harness.SeedUser("analyst.a");
        var analystB = await harness.SeedUser("analyst.b");

        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RoutineEnvironmental, (template, SpecificationAnalysisType.Microbial));

        var created = await harness.TestRequests.CreateTestRequest(
            new CreateTestRequestRequest
            {
                Type = TestRequestType.RoutineEnvironmental,
                SpecificationId = specification.Id,
                ScheduleOrigin = TestRequestScheduleOrigin.Scheduled,
                ArNumber = "ARD-1",
                Subjects = [new CreateTestRequestSubjectRequest { SubjectRef = "SF-91" }]
            },
            Guid.NewGuid());

        var instanceId = created.Value.Subjects.Single().WorksheetInstances.Single().Id;

        await harness.WorksheetInstances.Assign(
            instanceId, new AssignWorksheetInstanceRequest { AssignedToId = analystA.Id }, Guid.NewGuid());

        var result = await harness.WorksheetInstances.Start(instanceId, analystB.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheetInstance.NotTheAssignee", result.Error.Code);

        // And the assignee themselves is let through, so the rejection is about assignment
        // rather than about the worksheet being unstartable.
        var allowed = await harness.WorksheetInstances.Start(instanceId, analystA.Id);
        Assert.True(allowed.IsSuccess, allowed.Error?.Description);
    }

    /// <summary>Entering values is under the same rule as starting.</summary>
    [Fact]
    public async Task Another_analyst_cannot_enter_values_on_someone_elses_worksheet()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields(
            "WS/EM/2", WorksheetCategory.Microbial, Field("viables", WorksheetFieldType.ColonyCount));

        var (instanceId, _) = await StartedWorksheet(harness, template);
        var intruder = await harness.SeedUser("analyst.b");

        var result = await harness.WorksheetInstances.SaveValues(
            instanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues = [new WorksheetFieldValueEntry { FieldKey = "viables", Value = "4" }]
            },
            intruder.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheetInstance.NotTheAssignee", result.Error.Code);
    }

    // -----------------------------------------------------------------------
    // Criterion 3 — reassignment
    // -----------------------------------------------------------------------

    /// <summary>
    /// Criterion 3 — reassignment moves who may act next, audits itself, and touches nothing
    /// else: the value entered by the first analyst is still attributed to them, and no
    /// approval record is created, because reassignment is not a signature.
    /// </summary>
    [Fact]
    public async Task Reassignment_preserves_entered_values_is_audited_and_is_not_an_approval()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields(
            "WS/EM/3", WorksheetCategory.Microbial, Field("viables", WorksheetFieldType.ColonyCount));

        var (instanceId, analystA) = await StartedWorksheet(harness, template);
        var analystB = await harness.SeedUser("analyst.b");
        var manager = await harness.SeedUser("qc.manager");

        var entered = await harness.WorksheetInstances.SaveValues(
            instanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues = [new WorksheetFieldValueEntry { FieldKey = "viables", Value = "12" }]
            },
            analystA.Id);

        Assert.True(entered.IsSuccess, entered.Error?.Description);

        var reassigned = await harness.WorksheetInstances.Reassign(
            instanceId,
            new ReassignWorksheetInstanceRequest
            {
                AssignedToId = analystB.Id,
                Reason = "Analyst A is off shift."
            },
            manager.Id);

        Assert.True(reassigned.IsSuccess, reassigned.Error?.Description);
        Assert.Equal(analystB.Id, reassigned.Value.AssignedToId);

        // The status is untouched — reassignment never rewinds work.
        Assert.Equal(WorksheetInstanceStatus.InProgress, reassigned.Value.Status);

        // The value still belongs to whoever actually typed it.
        var value = await harness.Db.QcWorksheetFieldValues
            .SingleAsync(item => item.WorksheetInstanceId == instanceId && item.FieldKey == "viables");

        Assert.Equal(analystA.Id, value.EnteredById);
        Assert.Equal("12", value.Value);

        var audit = await harness.Db.QcWorksheetInstanceReassignments
            .SingleAsync(item => item.WorksheetInstanceId == instanceId);

        Assert.Equal(analystA.Id, audit.FromUserId);
        Assert.Equal(analystB.Id, audit.ToUserId);
        Assert.Equal(manager.Id, audit.ReassignedById);
        Assert.Equal("Analyst A is off shift.", audit.Reason);

        // Not routed through the approval engine: no QcApproval row, in either direction.
        Assert.Empty(await harness.Db.QcApprovals
            .Where(item => item.EntityType == QcApprovalEntityTypes.WorksheetInstance
                && item.EntityId == instanceId)
            .ToListAsync());
    }

    /// <summary>Reassignment still demands a reason — it is an audited action, not a silent one.</summary>
    [Fact]
    public async Task Reassignment_without_a_reason_is_rejected()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("WS/EM/4", WorksheetCategory.Microbial, "viables");
        var (instanceId, _) = await StartedWorksheet(harness, template);
        var analystB = await harness.SeedUser("analyst.b");

        var result = await harness.WorksheetInstances.Reassign(
            instanceId,
            new ReassignWorksheetInstanceRequest { AssignedToId = analystB.Id, Reason = "  " },
            Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheet.ReasonForChangeRequired", result.Error.Code);
    }

    // -----------------------------------------------------------------------
    // Criterion 4 — the header block is computed, never entered
    // -----------------------------------------------------------------------

    /// <summary>
    /// Criterion 4 — the header is rendered from the round, the subject and the pinned
    /// documents. Nothing on it is a worksheet field, so there is no key an analyst could aim a
    /// value at; a Constant-mode field is refused for the same reason.
    /// </summary>
    [Fact]
    public async Task The_header_block_is_computed_and_has_no_write_path()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields(
            "WS/EM/5",
            WorksheetCategory.Microbial,
            Field("viables", WorksheetFieldType.ColonyCount),
            Field("method_temp", WorksheetFieldType.IncubationTemperature, WorksheetFieldMode.Constant, 2));

        var (instanceId, analyst) = await StartedWorksheet(harness, template);

        var detail = await harness.WorksheetInstances.GetWorksheetInstance(instanceId);
        Assert.True(detail.IsSuccess, detail.Error?.Description);

        // Rendered from the subject and the round, not from anything typed.
        Assert.Equal("SF-91", detail.Value.Header.SubjectRef);
        Assert.Equal("Deblistering-2", detail.Value.Header.SubjectLabel);
        Assert.Equal("ARD-2609124", detail.Value.Header.ArNumber);
        Assert.Equal("Rev 1", detail.Value.Header.SpecificationRevision);
        Assert.Equal(1, detail.Value.Header.SpecificationVersion);

        // Water/EM carries no manufacturing or expiry date, so the header leaves them blank
        // rather than inventing them.
        Assert.Null(detail.Value.Header.ManufacturingDate);
        Assert.Null(detail.Value.Header.ExpiryDate);

        // There is no field key for a header value: aiming one at SubjectRef is simply unknown.
        var subjectRef = await harness.WorksheetInstances.SaveValues(
            instanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues = [new WorksheetFieldValueEntry { FieldKey = "SubjectRef", Value = "SF-99" }]
            },
            analyst.Id);

        Assert.False(subjectRef.IsSuccess);
        Assert.Equal("QcWorksheetInstance.UnknownFieldKey", subjectRef.Error.Code);

        // And a fixed method parameter on the template cannot be typed over either.
        var constant = await harness.WorksheetInstances.SaveValues(
            instanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues = [new WorksheetFieldValueEntry { FieldKey = "method_temp", Value = "45" }]
            },
            analyst.Id);

        Assert.False(constant.IsSuccess);
        Assert.Equal("QcWorksheetInstance.FieldNotEnterable", constant.Error.Code);

        // The subject is unchanged by either attempt.
        var subject = await harness.Db.QcTestRequestSubjects.SingleAsync();
        Assert.Equal("SF-91", subject.SubjectRef);
    }

    // -----------------------------------------------------------------------
    // Criterion 5 — the instrument gate
    // -----------------------------------------------------------------------

    /// <summary>
    /// Criterion 5 — equipment whose calibration is overdue is refused outright, and the same
    /// entry succeeds once the register says it is back in date.
    /// </summary>
    [Fact]
    public async Task An_instrument_out_of_calibration_is_blocked_until_the_register_is_current()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields(
            "WS/CHEM/1", WorksheetCategory.Microbial, Field("balance", WorksheetFieldType.Instrument));

        var (instanceId, analyst) = await StartedWorksheet(harness, template);

        var equipment = await harness.SeedEquipment("Balance-01", DateTime.UtcNow.AddDays(-1));

        var write = new SaveWorksheetValuesRequest
        {
            FieldValues =
            [
                new WorksheetFieldValueEntry { FieldKey = "balance", Value = equipment.Id.ToString() }
            ]
        };

        var blocked = await harness.WorksheetInstances.SaveValues(instanceId, write, analyst.Id);

        Assert.False(blocked.IsSuccess);
        Assert.Equal("QcWorksheetInstance.InstrumentCalibrationExpired", blocked.Error.Code);

        // Nothing was written: the gate runs before anything is persisted.
        Assert.Empty(await harness.Db.QcWorksheetFieldValues
            .Where(value => value.WorksheetInstanceId == instanceId)
            .ToListAsync());

        equipment.CalibrationDueDate = DateTime.UtcNow.AddMonths(6);
        await harness.Db.SaveChangesAsync();

        var allowed = await harness.WorksheetInstances.SaveValues(instanceId, write, analyst.Id);

        Assert.True(allowed.IsSuccess, allowed.Error?.Description);
    }

    /// <summary>
    /// Equipment with no calibration date on record cannot demonstrate it is in date, so it is
    /// blocked on the same footing as expired equipment.
    /// </summary>
    [Fact]
    public async Task An_instrument_with_no_calibration_date_is_blocked()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields(
            "WS/CHEM/2", WorksheetCategory.Microbial, Field("balance", WorksheetFieldType.Instrument));

        var (instanceId, analyst) = await StartedWorksheet(harness, template);
        var equipment = await harness.SeedEquipment("Balance-02", null);

        var result = await harness.WorksheetInstances.SaveValues(
            instanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues =
                [
                    new WorksheetFieldValueEntry { FieldKey = "balance", Value = equipment.Id.ToString() }
                ]
            },
            analyst.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheetInstance.InstrumentCalibrationUnknown", result.Error.Code);
    }

    // -----------------------------------------------------------------------
    // Criterion 6 — the reagent gate
    // -----------------------------------------------------------------------

    /// <summary>
    /// Criterion 6 — an expired reagent lot is refused, and the same reagent with a lot that is
    /// still in date goes through. The expiry checked is the one just entered, because no
    /// catalog record tracks reagent lots.
    /// </summary>
    [Fact]
    public async Task An_expired_reagent_lot_is_blocked_and_an_in_date_one_is_accepted()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields(
            "WS/CHEM/3", WorksheetCategory.Microbial, Field("cetrimide", WorksheetFieldType.Reagent));

        var (instanceId, analyst) = await StartedWorksheet(harness, template);
        var reagent = await harness.SeedReagent("Cetrimide agar");

        SaveWorksheetValuesRequest Entry(string expiry) => new()
        {
            FieldValues =
            [
                new WorksheetFieldValueEntry
                {
                    FieldKey = "cetrimide",
                    ColumnKey = QcWorksheetValueColumns.ReagentId,
                    Value = reagent.Id.ToString()
                },
                new WorksheetFieldValueEntry
                {
                    FieldKey = "cetrimide",
                    ColumnKey = QcWorksheetValueColumns.BatchNo,
                    Value = "CT-2026-07"
                },
                new WorksheetFieldValueEntry
                {
                    FieldKey = "cetrimide",
                    ColumnKey = QcWorksheetValueColumns.ExpiryDate,
                    Value = expiry
                }
            ]
        };

        var expired = await harness.WorksheetInstances.SaveValues(
            instanceId, Entry(DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd")), analyst.Id);

        Assert.False(expired.IsSuccess);
        Assert.Equal("QcWorksheetInstance.ReagentExpired", expired.Error.Code);

        var inDate = await harness.WorksheetInstances.SaveValues(
            instanceId, Entry(DateTime.UtcNow.AddMonths(3).ToString("yyyy-MM-dd")), analyst.Id);

        Assert.True(inDate.IsSuccess, inDate.Error?.Description);

        // All three parts of the entry are recorded against the one field key.
        var values = await harness.Db.QcWorksheetFieldValues
            .Where(value => value.WorksheetInstanceId == instanceId && value.FieldKey == "cetrimide")
            .ToListAsync();

        Assert.Equal(3, values.Count);
    }

    /// <summary>
    /// The gate runs again at submission, so a reagent that expires between entry and
    /// submission still cannot be signed off.
    /// </summary>
    [Fact]
    public async Task The_reagent_gate_runs_again_at_submission()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields(
            "WS/CHEM/4", WorksheetCategory.Microbial, Field("cetrimide", WorksheetFieldType.Reagent));

        var (instanceId, analyst) = await StartedWorksheet(harness, template);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);
        var reagent = await harness.SeedReagent("Cetrimide agar");

        await harness.WorksheetInstances.SaveValues(
            instanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues =
                [
                    new WorksheetFieldValueEntry
                    {
                        FieldKey = "cetrimide",
                        ColumnKey = QcWorksheetValueColumns.ReagentId,
                        Value = reagent.Id.ToString()
                    },
                    new WorksheetFieldValueEntry
                    {
                        FieldKey = "cetrimide",
                        ColumnKey = QcWorksheetValueColumns.BatchNo,
                        Value = "CT-2026-07"
                    },
                    new WorksheetFieldValueEntry
                    {
                        FieldKey = "cetrimide",
                        ColumnKey = QcWorksheetValueColumns.ExpiryDate,
                        Value = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd")
                    }
                ]
            },
            analyst.Id);

        // The lot lapses between entry and submission.
        var expiry = await harness.Db.QcWorksheetFieldValues.SingleAsync(value =>
            value.WorksheetInstanceId == instanceId
            && value.ColumnKey == QcWorksheetValueColumns.ExpiryDate);

        expiry.Value = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
        await harness.Db.SaveChangesAsync();

        var submitted = await harness.WorksheetInstances.Submit(instanceId, analyst.Id);

        Assert.False(submitted.IsSuccess);
        Assert.Equal("QcWorksheetInstance.ReagentExpired", submitted.Error.Code);
    }

    // -----------------------------------------------------------------------
    // Criterion 7 — ReferencedResult resolves only against a reviewed source
    // -----------------------------------------------------------------------

    /// <summary>
    /// Criterion 7 — a submitted-but-unreviewed source does not resolve; the same source, once
    /// reviewed, does. A merely submitted result is somebody's unchecked entry, and pulling it
    /// into a second worksheet would launder it into an approved-looking value.
    /// </summary>
    [Fact]
    public async Task A_referenced_result_resolves_only_once_its_source_is_reviewed()
    {
        using var harness = new QcWorksheetTestContext();

        // The source: a media qualification worksheet, keyed by the media batch number it
        // records against its own reagent field.
        var sourceTemplate = await harness.SeedTemplateWithFields(
            "WS/MQ/1",
            WorksheetCategory.MediaQualification,
            Field("media", WorksheetFieldType.Reagent),
            Field("growth_promotion", WorksheetFieldType.Result, WorksheetFieldMode.Entry, 2));

        var dependentTemplate = await harness.SeedTemplateWithFields(
            "WS/MICRO/1",
            WorksheetCategory.Microbial,
            Field("media", WorksheetFieldType.Reagent),
            new WorksheetField
            {
                FieldKey = "media_qualification",
                Label = "Media qualification",
                Type = WorksheetFieldType.ReferencedResult,
                Mode = WorksheetFieldMode.Entry,
                Order = 2,
                ReferencedResultSourceTemplateId = sourceTemplate.Id,
                ReferencedResultSourceFieldKey = "growth_promotion",
                ReferencedResultResolutionFieldKey = "media"
            });

        var (instanceId, analyst) = await StartedWorksheet(harness, dependentTemplate);
        var reagent = await harness.SeedReagent("TSA plates");

        // The analyst records which media lot they used — the lookup key.
        await harness.WorksheetInstances.SaveValues(
            instanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues =
                [
                    new WorksheetFieldValueEntry
                    {
                        FieldKey = "media",
                        ColumnKey = QcWorksheetValueColumns.ReagentId,
                        Value = reagent.Id.ToString()
                    },
                    new WorksheetFieldValueEntry
                    {
                        FieldKey = "media",
                        ColumnKey = QcWorksheetValueColumns.BatchNo,
                        Value = "MB-4417"
                    },
                    new WorksheetFieldValueEntry
                    {
                        FieldKey = "media",
                        ColumnKey = QcWorksheetValueColumns.ExpiryDate,
                        Value = DateTime.UtcNow.AddMonths(2).ToString("yyyy-MM-dd")
                    }
                ]
            },
            analyst.Id);

        // The source worksheet for that same media batch, submitted but not yet reviewed. It is
        // seeded directly: what is under test is resolution, not the submission path.
        var source = new WorksheetInstance
        {
            Id = Guid.NewGuid(),
            TestRequestSubjectId = await harness.Db.QcTestRequestSubjects
                .Select(subject => subject.Id).FirstAsync(),
            WorksheetTemplateId = sourceTemplate.Id,
            WorksheetTemplateVersion = sourceTemplate.Version,
            AnalysisType = SpecificationAnalysisType.Microbial,
            Status = WorksheetInstanceStatus.Submitted,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        harness.Db.QcWorksheetInstances.Add(source);
        harness.Db.QcWorksheetFieldValues.AddRange(
            new WorksheetFieldValue
            {
                Id = Guid.NewGuid(),
                WorksheetInstanceId = source.Id,
                FieldKey = "media",
                ColumnKey = QcWorksheetValueColumns.BatchNo,
                Value = "MB-4417",
                EnteredById = analyst.Id,
                EnteredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new WorksheetFieldValue
            {
                Id = Guid.NewGuid(),
                WorksheetInstanceId = source.Id,
                FieldKey = "growth_promotion",
                Value = "Complies",
                EnteredById = analyst.Id,
                EnteredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });

        await harness.Db.SaveChangesAsync();

        var pending = await harness.WorksheetInstances.GetWorksheetInstance(instanceId);
        var pendingField = pending.Value.Sections
            .SelectMany(section => section.Fields)
            .Single(field => field.FieldKey == "media_qualification");

        Assert.False(pendingField.ReferencedResult.Resolved);
        Assert.Equal("MB-4417", pendingField.ReferencedResult.ResolutionValue);
        Assert.Contains("No matching qualification", pendingField.ReferencedResult.Message);

        // It is pending, not broken — it must not block the rest of the worksheet.
        Assert.Null(pendingField.ReferencedResult.Value);

        // The source is reviewed. Now it resolves.
        source.Status = WorksheetInstanceStatus.Reviewed;
        await harness.Db.SaveChangesAsync();

        var resolvedDetail = await harness.WorksheetInstances.GetWorksheetInstance(instanceId);
        var resolvedField = resolvedDetail.Value.Sections
            .SelectMany(section => section.Fields)
            .Single(field => field.FieldKey == "media_qualification");

        Assert.True(resolvedField.ReferencedResult.Resolved);
        Assert.Equal("Complies", resolvedField.ReferencedResult.Value);
        Assert.Equal(source.Id, resolvedField.ReferencedResult.ResolvedFromInstanceId);

        // It is resolved, never entered: there is no write path to it.
        Assert.True(resolvedField.ReadOnly);

        var typed = await harness.WorksheetInstances.SaveValues(
            instanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues =
                [
                    new WorksheetFieldValueEntry { FieldKey = "media_qualification", Value = "Complies" }
                ]
            },
            analyst.Id);

        Assert.False(typed.IsSuccess);
        Assert.Equal("QcWorksheetInstance.ReferencedResultNotEnterable", typed.Error.Code);
    }

    /// <summary>
    /// An unresolved referenced result blocks submission — the one thing it is allowed to
    /// block.
    /// </summary>
    [Fact]
    public async Task An_unresolved_referenced_result_blocks_submission()
    {
        using var harness = new QcWorksheetTestContext();
        var sourceTemplate = await harness.SeedTemplateWithFields(
            "WS/MQ/2", WorksheetCategory.MediaQualification, Field("growth_promotion", WorksheetFieldType.Result));

        var dependentTemplate = await harness.SeedTemplateWithFields(
            "WS/MICRO/2",
            WorksheetCategory.Microbial,
            Field("tamc", WorksheetFieldType.ColonyCount),
            new WorksheetField
            {
                FieldKey = "media_qualification",
                Label = "Media qualification",
                Type = WorksheetFieldType.ReferencedResult,
                Mode = WorksheetFieldMode.Entry,
                Order = 2,
                ReferencedResultSourceTemplateId = sourceTemplate.Id,
                ReferencedResultSourceFieldKey = "growth_promotion",
                ReferencedResultResolutionFieldKey = "media"
            });

        var (instanceId, analyst) = await StartedWorksheet(harness, dependentTemplate);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        await harness.WorksheetInstances.SaveValues(
            instanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues = [new WorksheetFieldValueEntry { FieldKey = "tamc", Value = "3" }]
            },
            analyst.Id);

        var result = await harness.WorksheetInstances.Submit(instanceId, analyst.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheetInstance.ReferencedResultUnresolved", result.Error.Code);
    }

    // -----------------------------------------------------------------------
    // Submission and review
    // -----------------------------------------------------------------------

    /// <summary>
    /// The full execution path: submit puts the worksheet into the reviewers' queue through the
    /// existing approval engine, and an approved review is a re-authenticated signature
    /// recorded in the same shared QcApproval table Milestones 1 and 2 use.
    /// </summary>
    [Fact]
    public async Task Submitting_and_reviewing_records_a_reauthenticated_signature()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields(
            "WS/EM/6", WorksheetCategory.Microbial, Field("viables", WorksheetFieldType.ColonyCount));

        var (instanceId, analyst) = await StartedWorksheet(harness, template);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        await harness.WorksheetInstances.SaveValues(
            instanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues = [new WorksheetFieldValueEntry { FieldKey = "viables", Value = "7" }]
            },
            analyst.Id);

        var submitted = await harness.WorksheetInstances.Submit(instanceId, analyst.Id);

        Assert.True(submitted.IsSuccess, submitted.Error?.Description);
        Assert.Equal(WorksheetInstanceStatus.Submitted, submitted.Value.Status);
        Assert.NotNull(submitted.Value.SubmittedAt);

        // Submission itself signs nothing — the stage row is pending until someone reviews.
        var pending = await harness.Db.QcApprovals
            .SingleAsync(item => item.EntityType == QcApprovalEntityTypes.WorksheetInstance
                && item.EntityId == instanceId);

        Assert.Null(pending.ReauthConfirmedAt);

        var wrongPassword = await harness.WorksheetInstances.Review(
            instanceId,
            new ReviewWorksheetInstanceRequest { Approve = true, Password = "not-my-password" },
            harness.Approver.Id,
            []);

        Assert.False(wrongPassword.IsSuccess);
        Assert.Equal("QcWorksheet.ReauthenticationFailed", wrongPassword.Error.Code);

        var reviewed = await harness.WorksheetInstances.Review(
            instanceId,
            new ReviewWorksheetInstanceRequest
            {
                Approve = true,
                Password = QcWorksheetTestContext.CorrectPassword,
                Comments = "Reviewed by"
            },
            harness.Approver.Id,
            []);

        Assert.True(reviewed.IsSuccess, reviewed.Error?.Description);
        Assert.Equal(WorksheetInstanceStatus.Reviewed, reviewed.Value.Status);
        Assert.True(reviewed.Value.Approved);

        var signed = await harness.Db.QcApprovals
            .SingleAsync(item => item.EntityType == QcApprovalEntityTypes.WorksheetInstance
                && item.EntityId == instanceId);

        Assert.NotNull(signed.ReauthConfirmedAt);
        Assert.Equal(harness.Approver.Id, signed.ApprovedById);

        // The round follows its worksheets without being moved by hand.
        var round = await harness.Db.QcTestRequests.SingleAsync();
        Assert.Equal(TestRequestStatus.UnderReview, round.Status);
    }

    /// <summary>
    /// Segregation of duties — the analyst who entered and submitted the results cannot review
    /// them, even holding review rights and giving the correct password. The refusal comes
    /// before any signature is taken, so nothing is signed either.
    /// </summary>
    [Fact]
    public async Task The_analyst_who_submitted_a_worksheet_cannot_review_it()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields(
            "WS/EM/11", WorksheetCategory.Microbial, Field("viables", WorksheetFieldType.ColonyCount));

        var (instanceId, analyst) = await StartedWorksheet(harness, template);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        // The analyst gets a real password of their own, so the refusal cannot be mistaken for
        // a failed re-authentication.
        const string analystPassword = "Analyst-Password-1!";
        analyst.PasswordHash = new PasswordHasher<User>().HashPassword(analyst, analystPassword);
        await harness.Db.SaveChangesAsync();

        await harness.WorksheetInstances.SaveValues(
            instanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues = [new WorksheetFieldValueEntry { FieldKey = "viables", Value = "7" }]
            },
            analyst.Id);

        await harness.WorksheetInstances.Submit(instanceId, analyst.Id);

        var selfReview = await harness.WorksheetInstances.Review(
            instanceId,
            new ReviewWorksheetInstanceRequest
            {
                Approve = true,
                Password = analystPassword,
                Comments = "Looks fine to me."
            },
            analyst.Id,
            []);

        Assert.False(selfReview.IsSuccess);
        Assert.Equal("QcWorksheetInstance.CannotReviewOwnWork", selfReview.Error.Code);

        // The worksheet is untouched: still submitted, still unapproved.
        var instance = await harness.Db.QcWorksheetInstances.SingleAsync(item => item.Id == instanceId);
        Assert.Equal(WorksheetInstanceStatus.Submitted, instance.Status);
        Assert.False(instance.Approved);

        // Nothing was signed — the stage row is still pending.
        var stage = await harness.Db.QcApprovals
            .SingleAsync(item => item.EntityType == QcApprovalEntityTypes.WorksheetInstance
                && item.EntityId == instanceId);

        Assert.Null(stage.ReauthConfirmedAt);

        // An independent reviewer still gets through, so the refusal is about who did the work
        // rather than about the worksheet being unreviewable.
        var independent = await harness.WorksheetInstances.Review(
            instanceId,
            new ReviewWorksheetInstanceRequest
            {
                Approve = true,
                Password = QcWorksheetTestContext.CorrectPassword,
                Comments = "Reviewed by"
            },
            harness.Approver.Id,
            []);

        Assert.True(independent.IsSuccess, independent.Error?.Description);
        Assert.Equal(WorksheetInstanceStatus.Reviewed, independent.Value.Status);
    }

    /// <summary>
    /// Handing the worksheet on does not launder the reviewer's independence: a previous
    /// assignee still held the work, and the reassignment log is what proves it.
    /// </summary>
    [Fact]
    public async Task A_previous_assignee_cannot_review_the_worksheet_they_handed_on()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields(
            "WS/EM/12", WorksheetCategory.Microbial, Field("viables", WorksheetFieldType.ColonyCount));

        var (instanceId, analystA) = await StartedWorksheet(harness, template);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var analystB = await harness.SeedUser("analyst.b");

        const string password = "Analyst-A-Password-1!";
        analystA.PasswordHash = new PasswordHasher<User>().HashPassword(analystA, password);
        await harness.Db.SaveChangesAsync();

        await harness.WorksheetInstances.Reassign(
            instanceId,
            new ReassignWorksheetInstanceRequest { AssignedToId = analystB.Id, Reason = "Shift handover." },
            Guid.NewGuid());

        await harness.WorksheetInstances.SaveValues(
            instanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues = [new WorksheetFieldValueEntry { FieldKey = "viables", Value = "7" }]
            },
            analystB.Id);

        await harness.WorksheetInstances.Submit(instanceId, analystB.Id);

        var result = await harness.WorksheetInstances.Review(
            instanceId,
            new ReviewWorksheetInstanceRequest { Approve = true, Password = password },
            analystA.Id,
            []);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheetInstance.CannotReviewOwnWork", result.Error.Code);
    }

    /// <summary>
    /// Every correction cycle is its own audit row — reason, who returned it, when, and the
    /// review round it interrupted. A worksheet that goes round the loop twice keeps both rows:
    /// the history is the record, not just the latest bounce.
    /// </summary>
    [Fact]
    public async Task Every_correction_cycle_is_audited_and_the_history_is_retained()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields(
            "WS/EM/13", WorksheetCategory.Microbial, Field("viables", WorksheetFieldType.ColonyCount));

        var (instanceId, analyst) = await StartedWorksheet(harness, template);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var reviewer = harness.Approver;

        async Task EnterAndSubmit(string value)
        {
            await harness.WorksheetInstances.SaveValues(
                instanceId,
                new SaveWorksheetValuesRequest
                {
                    FieldValues = [new WorksheetFieldValueEntry { FieldKey = "viables", Value = value }]
                },
                analyst.Id);

            var submitted = await harness.WorksheetInstances.Submit(instanceId, analyst.Id);
            Assert.True(submitted.IsSuccess, submitted.Error?.Description);
        }

        // Cycle one: the unsigned return-for-correction action.
        await EnterAndSubmit("7");

        var firstReturn = await harness.WorksheetInstances.ReturnForCorrection(
            instanceId,
            new ReturnWorksheetForCorrectionRequest { Reason = "Colony count needs a second read." },
            reviewer.Id);

        Assert.True(firstReturn.IsSuccess, firstReturn.Error?.Description);
        Assert.Equal(WorksheetInstanceStatus.InProgress, firstReturn.Value.Status);

        // Cycle two: the reviewer's signed decision to decline.
        await EnterAndSubmit("8");

        var secondReturn = await harness.WorksheetInstances.Review(
            instanceId,
            new ReviewWorksheetInstanceRequest
            {
                Approve = false,
                Password = QcWorksheetTestContext.CorrectPassword,
                Comments = "Incubation period is not recorded."
            },
            reviewer.Id,
            []);

        Assert.True(secondReturn.IsSuccess, secondReturn.Error?.Description);
        Assert.Equal(WorksheetInstanceStatus.InProgress, secondReturn.Value.Status);

        // Both cycles survive — the first was not overwritten by the second.
        var returns = await harness.Db.QcWorksheetInstanceCorrectionReturns
            .Where(item => item.WorksheetInstanceId == instanceId)
            .OrderBy(item => item.ReturnedAt)
            .ToListAsync();

        Assert.Equal(2, returns.Count);

        Assert.Equal("Colony count needs a second read.", returns[0].Reason);
        Assert.Equal(reviewer.Id, returns[0].ReturnedById);
        Assert.NotEqual(default, returns[0].ReturnedAt);

        Assert.Equal("Incubation period is not recorded.", returns[1].Reason);
        Assert.Equal(reviewer.Id, returns[1].ReturnedById);

        // The declined review is a signature; the standalone action is not.
        Assert.False(returns[0].Signed);
        Assert.True(returns[1].Signed);

        // Each return names the review round it interrupted, so the log reads against the
        // signature trail.
        Assert.True(returns[1].ApprovalRound >= returns[0].ApprovalRound);

        // And the same history comes back on the worksheet's own detail read.
        var detail = await harness.WorksheetInstances.GetWorksheetInstance(instanceId);
        Assert.Equal(2, detail.Value.CorrectionReturns.Count);
        Assert.Equal("Colony count needs a second read.", detail.Value.CorrectionReturns[0].Reason);
        Assert.Equal("Incubation period is not recorded.", detail.Value.CorrectionReturns[1].Reason);
    }

    /// <summary>
    /// Returning a worksheet for correction sends it back to the same analyst, in progress,
    /// with everything they entered still there.
    /// </summary>
    [Fact]
    public async Task Returning_for_correction_keeps_the_assignee_and_the_entered_values()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields(
            "WS/EM/7", WorksheetCategory.Microbial, Field("viables", WorksheetFieldType.ColonyCount));

        var (instanceId, analyst) = await StartedWorksheet(harness, template);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        await harness.WorksheetInstances.SaveValues(
            instanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues = [new WorksheetFieldValueEntry { FieldKey = "viables", Value = "7" }]
            },
            analyst.Id);

        await harness.WorksheetInstances.Submit(instanceId, analyst.Id);

        var returned = await harness.WorksheetInstances.ReturnForCorrection(
            instanceId,
            new ReturnWorksheetForCorrectionRequest { Reason = "Colony count needs a second read." },
            harness.Approver?.Id ?? Guid.NewGuid());

        Assert.True(returned.IsSuccess, returned.Error?.Description);
        Assert.Equal(WorksheetInstanceStatus.InProgress, returned.Value.Status);
        Assert.Equal(analyst.Id, returned.Value.AssignedToId);
        Assert.Null(returned.Value.SubmittedAt);

        var value = await harness.Db.QcWorksheetFieldValues
            .SingleAsync(item => item.WorksheetInstanceId == instanceId);

        Assert.Equal("7", value.Value);
        Assert.Equal(analyst.Id, value.EnteredById);
    }

    /// <summary>
    /// A worksheet with an empty required field cannot be submitted: an incomplete worksheet
    /// reaching a reviewer is the failure this check exists to prevent.
    /// </summary>
    [Fact]
    public async Task A_worksheet_with_a_missing_value_cannot_be_submitted()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields(
            "WS/EM/8",
            WorksheetCategory.Microbial,
            Field("viables", WorksheetFieldType.ColonyCount),
            Field("remarks", WorksheetFieldType.LongText, WorksheetFieldMode.Entry, 2));

        var (instanceId, analyst) = await StartedWorksheet(harness, template);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        await harness.WorksheetInstances.SaveValues(
            instanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues = [new WorksheetFieldValueEntry { FieldKey = "viables", Value = "7" }]
            },
            analyst.Id);

        var result = await harness.WorksheetInstances.Submit(instanceId, analyst.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheetInstance.RequiredFieldMissing", result.Error.Code);
    }

    /// <summary>
    /// QC opts out of the approval engine's auto-approval fallback, so a worksheet cannot be
    /// submitted into a queue with no reviewer defined — it would otherwise become Reviewed
    /// with nobody having signed for it.
    /// </summary>
    [Fact]
    public async Task A_worksheet_cannot_be_submitted_without_a_configured_review_chain()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields(
            "WS/EM/9", WorksheetCategory.Microbial, Field("viables", WorksheetFieldType.ColonyCount));

        var (instanceId, analyst) = await StartedWorksheet(harness, template);

        await harness.WorksheetInstances.SaveValues(
            instanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues = [new WorksheetFieldValueEntry { FieldKey = "viables", Value = "7" }]
            },
            analyst.Id);

        var result = await harness.WorksheetInstances.Submit(instanceId, analyst.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheet.NoApprovalWorkflowConfigured", result.Error.Code);
    }

    /// <summary>
    /// "My Work" is scoped by assignment: an unassigned worksheet sits in nobody's queue, and
    /// reassigning one moves it between queues immediately.
    /// </summary>
    [Fact]
    public async Task My_work_is_scoped_by_assignment()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("WS/EM/10", WorksheetCategory.Microbial, "viables");
        var (instanceId, analystA) = await StartedWorksheet(harness, template);
        var analystB = await harness.SeedUser("analyst.b");

        var mine = await harness.WorksheetInstances.GetMyWork(analystA.Id, null);
        Assert.Single(mine.Value.Items);
        Assert.Equal(1, mine.Value.InProgressCount);

        var theirs = await harness.WorksheetInstances.GetMyWork(analystB.Id, null);
        Assert.Empty(theirs.Value.Items);

        await harness.WorksheetInstances.Reassign(
            instanceId,
            new ReassignWorksheetInstanceRequest { AssignedToId = analystB.Id, Reason = "Shift handover." },
            Guid.NewGuid());

        Assert.Empty((await harness.WorksheetInstances.GetMyWork(analystA.Id, null)).Value.Items);
        Assert.Single((await harness.WorksheetInstances.GetMyWork(analystB.Id, null)).Value.Items);
    }

    // -----------------------------------------------------------------------
    // Calculated fields are evaluated and persisted at submission
    // -----------------------------------------------------------------------

    /// <summary>
    /// A worksheet carrying a realistic microbiological CFU calculation: three plate counts in a
    /// table, averaged, then multiplied by the dilution factor.
    /// <para>
    /// The two Calculated fields are declared in the "wrong" order on purpose —
    /// <c>cfu_per_g</c> (order 3) references <c>mean_count</c> (order 4) — so the test proves
    /// evaluation resolves by dependency rather than by template order.
    /// </para>
    /// </summary>
    private static async Task<WorksheetTemplate> CfuTemplate(QcWorksheetTestContext harness, string code) =>
        await harness.SeedTemplateWithFields(
            code,
            WorksheetCategory.Microbial,
            new WorksheetField
            {
                FieldKey = "plate_counts",
                Label = "Plate counts",
                Type = WorksheetFieldType.Table,
                Mode = WorksheetFieldMode.Entry,
                Order = 1,
                ColumnDefinitions = """[{"label":"CFU","key":"cfu","type":"Number"}]"""
            },
            new WorksheetField
            {
                FieldKey = "dilution_factor",
                Label = "Dilution factor",
                Type = WorksheetFieldType.Dilution,
                Mode = WorksheetFieldMode.Entry,
                Order = 2
            },
            new WorksheetField
            {
                FieldKey = "cfu_per_g",
                Label = "CFU per g",
                Type = WorksheetFieldType.CfuCalculation,
                Mode = WorksheetFieldMode.Calculated,
                Order = 3,
                FormulaExpression = "{mean_count} * {dilution_factor}"
            },
            new WorksheetField
            {
                FieldKey = "mean_count",
                Label = "Mean plate count",
                Type = WorksheetFieldType.CalculatedValue,
                Mode = WorksheetFieldMode.Calculated,
                Order = 4,
                FormulaExpression = "AVG({plate_counts.cfu})"
            });

    private static SaveWorksheetValuesRequest PlateCounts(string dilutionFactor, params string[] counts) =>
        new()
        {
            FieldValues =
            [
                .. counts.Select((count, index) => new WorksheetFieldValueEntry
                {
                    FieldKey = "plate_counts",
                    RowIndex = index,
                    ColumnKey = "cfu",
                    Value = count
                }),
                new WorksheetFieldValueEntry { FieldKey = "dilution_factor", Value = dilutionFactor }
            ]
        };

    /// <summary>
    /// A submitted worksheet stores its calculated results as real
    /// <see cref="WorksheetFieldValue"/> rows, matching manual evaluation.
    /// <para>
    /// Before this, a Calculated field was evaluated only at template-save time to prove the
    /// formula parsed, and never against entered data — so the official record held the raw
    /// inputs and no computed result. A COA citing a CFU/g figure needs the number itself to
    /// exist, not merely to be rebuildable.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Calculated_fields_are_evaluated_against_entered_data_and_persisted_at_submission()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await CfuTemplate(harness, "WS/EM/11");
        var (instanceId, analyst) = await StartedWorksheet(harness, template);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        // AVG(30, 42, 48) = 40; 40 * 100 = 4000.
        await harness.WorksheetInstances.SaveValues(
            instanceId, PlateCounts("100", "30", "42", "48"), analyst.Id);

        // Nothing is computed before submission — the gap this closes.
        Assert.Empty(await harness.Db.QcWorksheetFieldValues
            .Where(value => value.WorksheetInstanceId == instanceId && value.FieldKey == "cfu_per_g")
            .ToListAsync());

        var result = await harness.WorksheetInstances.Submit(instanceId, analyst.Id);
        Assert.True(result.IsSuccess);

        var stored = await harness.Db.QcWorksheetFieldValues
            .Where(value => value.WorksheetInstanceId == instanceId)
            .ToListAsync();

        var mean = Assert.Single(stored, value => value.FieldKey == "mean_count");
        var cfu = Assert.Single(stored, value => value.FieldKey == "cfu_per_g");

        Assert.Equal("40", mean.Value);
        Assert.Equal("4000", cfu.Value);

        // Stored as an ordinary value row: attributed, timestamped, and indistinguishable in
        // shape from a typed one — system-computed rather than user-typed is the only difference.
        Assert.Equal(analyst.Id, cfu.EnteredById);
        Assert.NotEqual(default, cfu.EnteredAt);
        Assert.Null(cfu.RowIndex);
        Assert.Null(cfu.ColumnKey);

        // And it reaches the read model the COA and the reviewer both use.
        var detail = await harness.WorksheetInstances.GetWorksheetInstance(instanceId);
        var field = detail.Value.Sections
            .SelectMany(section => section.Fields)
            .Single(item => item.FieldKey == "cfu_per_g");

        Assert.Equal("4000", Assert.Single(field.Values).Value);
    }

    /// <summary>
    /// A Calculated field whose inputs will not evaluate refuses the submission outright, naming
    /// the field — and leaves nothing behind.
    /// <para>
    /// "TNTC" (too numerous to count) is a real entry on a real plate-count sheet: it satisfies
    /// the required-value check, so it gets past everything except the arithmetic. Silently
    /// skipping the field, or storing a blank result, would let the worksheet be submitted as
    /// complete while the number it exists to produce does not.
    /// </para>
    /// </summary>
    [Fact]
    public async Task A_calculated_field_that_cannot_be_evaluated_refuses_submission_and_persists_nothing()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await CfuTemplate(harness, "WS/EM/12");
        var (instanceId, analyst) = await StartedWorksheet(harness, template);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        await harness.WorksheetInstances.SaveValues(
            instanceId, PlateCounts("100", "30", "TNTC", "48"), analyst.Id);

        var result = await harness.WorksheetInstances.Submit(instanceId, analyst.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheetInstance.CalculatedFieldUnevaluatable", result.Error.Code);

        // The message names the field the analyst has to act on, and why — the root cause
        // (mean_count, which reads the poisoned column) rather than cfu_per_g downstream of it.
        Assert.Contains("mean_count", result.Error.Description);
        Assert.Contains("TNTC", result.Error.Description);

        var stored = await harness.Db.QcWorksheetFieldValues
            .Where(value => value.WorksheetInstanceId == instanceId)
            .ToListAsync();

        // Neither calculated field was written — not even the one that could have been computed
        // on its own. A refused submit is all-or-nothing.
        Assert.DoesNotContain(stored, value => value.FieldKey == "cfu_per_g");
        Assert.DoesNotContain(stored, value => value.FieldKey == "mean_count");

        var instance = await harness.Db.QcWorksheetInstances.SingleAsync(item => item.Id == instanceId);
        Assert.Equal(WorksheetInstanceStatus.InProgress, instance.Status);
        Assert.Null(instance.SubmittedAt);
    }

    /// <summary>
    /// A Calculated field has no analyst write path, for the same reason a Constant one has
    /// none: its value is derived at submission, so a typed value would only be overwritten.
    /// </summary>
    [Fact]
    public async Task A_calculated_field_cannot_be_typed_into()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await CfuTemplate(harness, "WS/EM/13");
        var (instanceId, analyst) = await StartedWorksheet(harness, template);

        var result = await harness.WorksheetInstances.SaveValues(
            instanceId,
            new SaveWorksheetValuesRequest
            {
                FieldValues = [new WorksheetFieldValueEntry { FieldKey = "cfu_per_g", Value = "999999" }]
            },
            analyst.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheetInstance.CalculatedFieldNotEnterable", result.Error.Code);
        Assert.Empty(await harness.Db.QcWorksheetFieldValues
            .Where(value => value.WorksheetInstanceId == instanceId)
            .ToListAsync());
    }
}
