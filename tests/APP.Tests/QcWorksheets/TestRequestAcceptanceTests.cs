using APP.Repository.QcWorksheets;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// Milestone 3 acceptance criterion 1 (the multi-subject round), plus the locked version-pinning
/// rule as it applies to a round and the worksheets it materializes.
/// </summary>
public class TestRequestAcceptanceTests
{
    private static CreateTestRequestRequest NewRequest(
        Specification specification,
        TestRequestType type,
        params string[] subjectRefs) => new()
    {
        Type = type,
        SpecificationId = specification.Id,
        ScheduleOrigin = TestRequestScheduleOrigin.Scheduled,
        ArNumber = "ARD-2609124",
        Subjects = subjectRefs
            .Select(reference => new CreateTestRequestSubjectRequest { SubjectRef = reference })
            .ToList()
    };

    // -----------------------------------------------------------------------
    // Criterion 1 — a round covers many subjects
    // -----------------------------------------------------------------------

    /// <summary>
    /// Criterion 1 — five Environmental sampling points in one round produce five worksheets,
    /// one each, Microbial only, because that is all the specification links.
    /// </summary>
    [Fact]
    public async Task Environmental_round_with_five_subjects_creates_one_microbial_worksheet_each()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate(
            "WS/EM/1", WorksheetCategory.Microbial, "airborne_viables");

        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RoutineEnvironmental,
            (template, SpecificationAnalysisType.Microbial));

        var result = await harness.TestRequests.CreateTestRequest(
            NewRequest(specification, TestRequestType.RoutineEnvironmental,
                "SF-91", "SF-92", "SF-93", "SF-94", "SF-95"),
            Guid.NewGuid());

        Assert.True(result.IsSuccess, result.Error?.Description);
        Assert.Equal(5, result.Value.Subjects.Count);
        Assert.Equal(5, result.Value.WorksheetInstanceCount);

        foreach (var subject in result.Value.Subjects)
        {
            var instance = Assert.Single(subject.WorksheetInstances);
            Assert.Equal(SpecificationAnalysisType.Microbial, instance.AnalysisType);
            Assert.Equal(WorksheetInstanceStatus.NotStarted, instance.Status);
            Assert.Null(instance.AssignedToId);
        }
    }

    /// <summary>
    /// A specification linking both tracks gives every subject two worksheets — the
    /// (Subject × WorksheetLink) rule, not one worksheet per subject.
    /// </summary>
    [Fact]
    public async Task A_specification_linking_both_tracks_gives_every_subject_two_worksheets()
    {
        using var harness = new QcWorksheetTestContext();
        var chemical = await harness.SeedEffectiveTemplate("WS/CHEM/1", WorksheetCategory.Chemical, "assay");
        var microbial = await harness.SeedEffectiveTemplate("WS/MICRO/1", WorksheetCategory.Microbial, "tamc");

        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RoutineWater,
            (chemical, SpecificationAnalysisType.Chemical),
            (microbial, SpecificationAnalysisType.Microbial));

        var result = await harness.TestRequests.CreateTestRequest(
            NewRequest(specification, TestRequestType.RoutineWater, "WP-01", "WP-02"),
            Guid.NewGuid());

        Assert.True(result.IsSuccess, result.Error?.Description);
        Assert.Equal(4, result.Value.WorksheetInstanceCount);
        Assert.All(result.Value.Subjects, subject => Assert.Equal(2, subject.WorksheetInstances.Count));
    }

    // -----------------------------------------------------------------------
    // Version pinning — the locked governance rule
    // -----------------------------------------------------------------------

    /// <summary>
    /// The round pins the specification version in force at creation, and the worksheet pins
    /// the template version that specification version linked.
    /// </summary>
    [Fact]
    public async Task A_round_pins_the_specification_version_and_its_worksheets_pin_the_template_version()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("WS/RM/1", WorksheetCategory.Chemical, "assay");

        // The template row is version 3; the link pins that number.
        template.Version = 3;
        await harness.Db.SaveChangesAsync();

        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RawMaterial, (template, SpecificationAnalysisType.Chemical));

        specification.Version = 2;
        specification.WorksheetLinks[0].WorksheetTemplateVersion = 3;
        await harness.Db.SaveChangesAsync();

        var result = await harness.TestRequests.CreateTestRequest(
            NewRequest(specification, TestRequestType.RawMaterial, "PT260901"), Guid.NewGuid());

        Assert.True(result.IsSuccess, result.Error?.Description);
        Assert.Equal(2, result.Value.SpecificationVersion);

        var instance = result.Value.Subjects.Single().WorksheetInstances.Single();
        Assert.Equal(3, instance.WorksheetTemplateVersion);
        Assert.Equal(template.Id, instance.WorksheetTemplateId);
    }

    /// <summary>
    /// The pin never moves. A newer specification version becoming Effective afterwards leaves
    /// an in-flight round exactly where it was — no in-flight upgrade, which is the whole
    /// point of the rule.
    /// </summary>
    [Fact]
    public async Task A_newer_specification_version_does_not_move_an_in_flight_round()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("WS/RM/2", WorksheetCategory.Chemical, "assay");
        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RawMaterial, (template, SpecificationAnalysisType.Chemical));

        var created = await harness.TestRequests.CreateTestRequest(
            NewRequest(specification, TestRequestType.RawMaterial, "PT260901"), Guid.NewGuid());

        Assert.True(created.IsSuccess, created.Error?.Description);
        Assert.Equal(1, created.Value.SpecificationVersion);

        // Version 2 is approved and takes over, superseding version 1.
        var successor = await harness.Specifications.CreateNewVersion(specification.Id, Guid.NewGuid());
        Assert.True(successor.IsSuccess, successor.Error?.Description);

        var draft = await harness.Db.QcSpecifications.SingleAsync(item => item.Id == successor.Value.Id);
        draft.Status = QcDocumentStatus.Effective;

        var original = await harness.Db.QcSpecifications.SingleAsync(item => item.Id == specification.Id);
        original.Status = QcDocumentStatus.Superseded;
        await harness.Db.SaveChangesAsync();

        var reread = await harness.TestRequests.GetTestRequest(created.Value.Id);

        Assert.True(reread.IsSuccess);
        Assert.Equal(specification.Id, reread.Value.SpecificationId);
        Assert.Equal(1, reread.Value.SpecificationVersion);
        Assert.Equal(2, draft.Version);
    }

    /// <summary>
    /// A subject added later runs the round's own pinned specification version, not whichever
    /// version is Effective by then.
    /// </summary>
    [Fact]
    public async Task A_subject_added_later_uses_the_rounds_pinned_specification()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("WS/EM/2", WorksheetCategory.Microbial, "viables");
        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RoutineEnvironmental, (template, SpecificationAnalysisType.Microbial));

        var created = await harness.TestRequests.CreateTestRequest(
            NewRequest(specification, TestRequestType.RoutineEnvironmental, "SF-91"), Guid.NewGuid());

        Assert.True(created.IsSuccess, created.Error?.Description);

        var added = await harness.TestRequests.AddSubjects(
            created.Value.Id,
            new AddTestRequestSubjectsRequest
            {
                Subjects = [new CreateTestRequestSubjectRequest { SubjectRef = "SF-92" }]
            },
            Guid.NewGuid());

        Assert.True(added.IsSuccess, added.Error?.Description);
        Assert.Equal(2, added.Value.Subjects.Count);
        Assert.Equal(2, added.Value.WorksheetInstanceCount);
        Assert.All(added.Value.Subjects, subject =>
            Assert.Equal(template.Id, subject.WorksheetInstances.Single().WorksheetTemplateId));
    }

    // -----------------------------------------------------------------------
    // Round validation and lifecycle
    // -----------------------------------------------------------------------

    /// <summary>An unscheduled round must say why it was raised.</summary>
    [Fact]
    public async Task An_unscheduled_round_without_a_reason_is_rejected()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("WS/RM/3", WorksheetCategory.Chemical, "assay");
        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RawMaterial, (template, SpecificationAnalysisType.Chemical));

        var request = NewRequest(specification, TestRequestType.RawMaterial, "PT260901");
        request.ScheduleOrigin = TestRequestScheduleOrigin.Unscheduled;

        var result = await harness.TestRequests.CreateTestRequest(request, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("QcTestRequest.UnscheduledReasonRequired", result.Error.Code);
    }

    /// <summary>A round's type and its specification must agree.</summary>
    [Fact]
    public async Task A_round_cannot_run_against_a_specification_for_a_different_category()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("WS/RM/4", WorksheetCategory.Chemical, "assay");
        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RawMaterial, (template, SpecificationAnalysisType.Chemical));

        var result = await harness.TestRequests.CreateTestRequest(
            NewRequest(specification, TestRequestType.Product, "PT260901"), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("QcTestRequest.SpecificationTypeMismatch", result.Error.Code);
    }

    /// <summary>
    /// Real testing runs against a controlled document somebody signed for, so a Draft
    /// specification cannot govern a round.
    /// </summary>
    [Fact]
    public async Task A_round_cannot_run_against_a_specification_that_is_not_effective()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("WS/RM/5", WorksheetCategory.Chemical, "assay");
        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RawMaterial, (template, SpecificationAnalysisType.Chemical));

        specification.Status = QcDocumentStatus.Draft;
        await harness.Db.SaveChangesAsync();

        var result = await harness.TestRequests.CreateTestRequest(
            NewRequest(specification, TestRequestType.RawMaterial, "PT260901"), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("QcTestRequest.SpecificationNotEffective", result.Error.Code);
    }

    /// <summary>Recording the sample moves the round out of Draft and stamps its subjects.</summary>
    [Fact]
    public async Task Recording_the_sample_moves_the_round_to_sampled()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("WS/EM/3", WorksheetCategory.Microbial, "viables");
        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RoutineEnvironmental, (template, SpecificationAnalysisType.Microbial));

        var created = await harness.TestRequests.CreateTestRequest(
            NewRequest(specification, TestRequestType.RoutineEnvironmental, "SF-91", "SF-92"),
            Guid.NewGuid());

        Assert.Equal(TestRequestStatus.Draft, created.Value.Status);

        var sampled = await harness.TestRequests.RecordSample(
            created.Value.Id, new RecordTestRequestSampleRequest(), Guid.NewGuid());

        Assert.True(sampled.IsSuccess, sampled.Error?.Description);
        Assert.Equal(TestRequestStatus.Sampled, sampled.Value.Status);
        Assert.All(sampled.Value.Subjects, subject => Assert.NotNull(subject.CollectedAt));
    }

    /// <summary>
    /// Points can be added while a round is still Draft or Sampled, and not after testing has
    /// started — which the derived round status reflects.
    /// </summary>
    [Fact]
    public async Task Subjects_cannot_be_added_once_testing_has_started()
    {
        using var harness = new QcWorksheetTestContext();
        var analyst = await harness.SeedUser("analyst.a");
        var template = await harness.SeedEffectiveTemplate("WS/EM/4", WorksheetCategory.Microbial, "viables");
        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RoutineEnvironmental, (template, SpecificationAnalysisType.Microbial));

        var created = await harness.TestRequests.CreateTestRequest(
            NewRequest(specification, TestRequestType.RoutineEnvironmental, "SF-91"), Guid.NewGuid());

        await harness.TestRequests.RecordSample(
            created.Value.Id, new RecordTestRequestSampleRequest(), Guid.NewGuid());

        var instanceId = created.Value.Subjects.Single().WorksheetInstances.Single().Id;

        await harness.WorksheetInstances.Assign(
            instanceId, new AssignWorksheetInstanceRequest { AssignedToId = analyst.Id }, Guid.NewGuid());

        await harness.WorksheetInstances.Start(instanceId, analyst.Id);

        var added = await harness.TestRequests.AddSubjects(
            created.Value.Id,
            new AddTestRequestSubjectsRequest
            {
                Subjects = [new CreateTestRequestSubjectRequest { SubjectRef = "SF-92" }]
            },
            Guid.NewGuid());

        Assert.False(added.IsSuccess);
        Assert.Equal("QcTestRequest.SubjectsLocked", added.Error.Code);
    }

    /// <summary>
    /// The round's status is derived from its worksheets: starting one puts the round into
    /// testing without anyone having to move it by hand.
    /// </summary>
    [Fact]
    public async Task Starting_a_worksheet_moves_the_round_into_testing()
    {
        using var harness = new QcWorksheetTestContext();
        var analyst = await harness.SeedUser("analyst.a");
        var template = await harness.SeedEffectiveTemplate("WS/EM/5", WorksheetCategory.Microbial, "viables");
        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RoutineEnvironmental, (template, SpecificationAnalysisType.Microbial));

        var created = await harness.TestRequests.CreateTestRequest(
            NewRequest(specification, TestRequestType.RoutineEnvironmental, "SF-91"), Guid.NewGuid());

        await harness.TestRequests.RecordSample(
            created.Value.Id, new RecordTestRequestSampleRequest(), Guid.NewGuid());

        var instanceId = created.Value.Subjects.Single().WorksheetInstances.Single().Id;

        await harness.WorksheetInstances.Assign(
            instanceId, new AssignWorksheetInstanceRequest { AssignedToId = analyst.Id }, Guid.NewGuid());

        var assigned = await harness.TestRequests.GetTestRequest(created.Value.Id);
        Assert.Equal(TestRequestStatus.Assigned, assigned.Value.Status);

        await harness.WorksheetInstances.Start(instanceId, analyst.Id);

        var testing = await harness.TestRequests.GetTestRequest(created.Value.Id);
        Assert.Equal(TestRequestStatus.InTesting, testing.Value.Status);
    }

    /// <summary>
    /// A sampling point group belongs to a Water/EM subject only — it drives which Alert/Action
    /// tier a result is judged against, which a material batch has no use for.
    /// </summary>
    [Fact]
    public async Task A_sampling_point_group_cannot_be_set_on_a_material_subject()
    {
        using var harness = new QcWorksheetTestContext();
        var group = await harness.SeedSamplingPointGroup("General Rooms");
        var template = await harness.SeedEffectiveTemplate("WS/RM/6", WorksheetCategory.Chemical, "assay");
        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RawMaterial, (template, SpecificationAnalysisType.Chemical));

        var request = NewRequest(specification, TestRequestType.RawMaterial, "PT260901");
        request.Subjects[0].SamplingPointGroupId = group.Id;

        var result = await harness.TestRequests.CreateTestRequest(request, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("QcTestRequest.SamplingPointGroupNotApplicable", result.Error.Code);
    }

    /// <summary>
    /// The derived round status, exercised directly across the shapes a real round passes
    /// through, including the mixed state where one worksheet is reviewed and another is not.
    /// </summary>
    [Theory]
    [InlineData(WorksheetInstanceStatus.NotStarted, WorksheetInstanceStatus.NotStarted, TestRequestStatus.Assigned)]
    [InlineData(WorksheetInstanceStatus.InProgress, WorksheetInstanceStatus.NotStarted, TestRequestStatus.InTesting)]
    [InlineData(WorksheetInstanceStatus.Submitted, WorksheetInstanceStatus.Submitted, TestRequestStatus.ResultsComplete)]
    [InlineData(WorksheetInstanceStatus.Reviewed, WorksheetInstanceStatus.Submitted, TestRequestStatus.UnderReview)]
    [InlineData(WorksheetInstanceStatus.Reviewed, WorksheetInstanceStatus.NotStarted, TestRequestStatus.InTesting)]
    public void Round_status_is_derived_from_its_worksheets(
        WorksheetInstanceStatus first, WorksheetInstanceStatus second, TestRequestStatus expected)
    {
        var assignee = Guid.NewGuid();

        var derived = QcTestRequestStatusCalculator.Derive([(first, assignee), (second, assignee)]);

        Assert.Equal(expected, derived);
    }
}
