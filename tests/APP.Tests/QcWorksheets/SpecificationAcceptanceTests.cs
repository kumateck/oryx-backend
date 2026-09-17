using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// Acceptance criteria 1-7 from the Milestone 2 brief, each named for the rule it proves.
/// Criterion 8 (coexistence) lives in <see cref="QcWorksheetCoexistenceTests"/>, which is
/// where Milestone 1 put the equivalent check.
/// </summary>
public class SpecificationAcceptanceTests
{
    private static CreateSpecificationRequest NewRequest(
        SpecificationAppliesTo appliesTo = SpecificationAppliesTo.RawMaterial,
        SpecificationStage? stage = null,
        QcRetestPolicy? retestPolicy = QcRetestPolicy.SameSample,
        string code = "QCD/SPEC/RM/001") => new()
    {
        Code = code,
        Name = "Ascorbic Acid",
        AppliesTo = appliesTo,
        Stage = stage,
        RetestPolicy = retestPolicy,
        WorksheetLinks = [],
        Characteristics = []
    };

    // -----------------------------------------------------------------------
    // Criterion 1 — WorksheetLink constraint
    // -----------------------------------------------------------------------

    /// <summary>
    /// Criterion 1 — at most one Chemical and one Microbial link per Specification.
    /// </summary>
    [Fact]
    public async Task Two_chemical_worksheet_links_are_rejected()
    {
        using var harness = new QcWorksheetTestContext();
        var first = await harness.SeedEffectiveTemplate("WS/CHEM/1", WorksheetCategory.Chemical, "assay");
        var second = await harness.SeedEffectiveTemplate("WS/CHEM/2", WorksheetCategory.Chemical, "assay");

        var request = NewRequest();
        request.WorksheetLinks =
        [
            new CreateSpecificationWorksheetLinkRequest
            {
                WorksheetTemplateId = first.Id,
                AnalysisType = SpecificationAnalysisType.Chemical
            },
            new CreateSpecificationWorksheetLinkRequest
            {
                WorksheetTemplateId = second.Id,
                AnalysisType = SpecificationAnalysisType.Chemical
            }
        ];

        var result = await harness.Specifications.CreateSpecification(request, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("QcSpecification.DuplicateAnalysisTypeLink", result.Error.Code);

        // And nothing was persisted by the rejected call.
        Assert.False(await harness.Db.QcSpecifications.AnyAsync());
    }

    /// <summary>Criterion 1 — one of each is the legitimate case and must still save.</summary>
    [Fact]
    public async Task One_chemical_and_one_microbial_link_is_allowed()
    {
        using var harness = new QcWorksheetTestContext();
        var chemical = await harness.SeedEffectiveTemplate("WS/CHEM", WorksheetCategory.Chemical, "assay");
        var microbial = await harness.SeedEffectiveTemplate("WS/MICRO", WorksheetCategory.Microbial, "tamc");

        var request = NewRequest();
        request.WorksheetLinks =
        [
            new CreateSpecificationWorksheetLinkRequest
            {
                WorksheetTemplateId = chemical.Id,
                AnalysisType = SpecificationAnalysisType.Chemical
            },
            new CreateSpecificationWorksheetLinkRequest
            {
                WorksheetTemplateId = microbial.Id,
                AnalysisType = SpecificationAnalysisType.Microbial
            }
        ];

        var result = await harness.Specifications.CreateSpecification(request, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.WorksheetLinks.Count);
    }

    /// <summary>
    /// Criterion 1, environmental half — a Routine Environmental specification only ever has
    /// a Microbial link.
    /// </summary>
    [Fact]
    public async Task Environmental_specification_rejects_a_chemical_link()
    {
        using var harness = new QcWorksheetTestContext();
        var chemical = await harness.SeedEffectiveTemplate("WS/CHEM", WorksheetCategory.Chemical, "assay");

        var request = NewRequest(SpecificationAppliesTo.RoutineEnvironmental);
        request.WorksheetLinks =
        [
            new CreateSpecificationWorksheetLinkRequest
            {
                WorksheetTemplateId = chemical.Id,
                AnalysisType = SpecificationAnalysisType.Chemical
            }
        ];

        var result = await harness.Specifications.CreateSpecification(request, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("QcSpecification.EnvironmentalIsMicrobialOnly", result.Error.Code);
    }

    // -----------------------------------------------------------------------
    // Criterion 2 — SourceFieldKey validated against the linked template
    // -----------------------------------------------------------------------

    /// <summary>
    /// Criterion 2, first half — a characteristic may only source a template this
    /// specification actually links, not merely any template that exists.
    /// </summary>
    [Fact]
    public async Task Characteristic_sourcing_an_unlinked_template_is_rejected()
    {
        using var harness = new QcWorksheetTestContext();
        var linked = await harness.SeedEffectiveTemplate("WS/CHEM", WorksheetCategory.Chemical, "assay");
        var unlinked = await harness.SeedEffectiveTemplate("WS/OTHER", WorksheetCategory.Chemical, "assay");

        var request = NewRequest();
        request.WorksheetLinks =
        [
            new CreateSpecificationWorksheetLinkRequest
            {
                WorksheetTemplateId = linked.Id,
                AnalysisType = SpecificationAnalysisType.Chemical
            }
        ];
        request.Characteristics =
        [
            new CreateSpecificationCharacteristicRequest
            {
                TestName = "Assay",
                AcceptanceCriteria = "95.0-105.0%",
                SourceWorksheetTemplateId = unlinked.Id,
                SourceFieldKey = "assay",
                DisplayOrder = 1
            }
        ];

        var result = await harness.Specifications.CreateSpecification(request, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("QcSpecification.CharacteristicTemplateNotLinked", result.Error.Code);
    }

    /// <summary>
    /// Criterion 2, second half — the field key must exist on that template's current
    /// Effective version.
    /// </summary>
    [Fact]
    public async Task Characteristic_with_an_unknown_field_key_is_rejected()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("WS/CHEM", WorksheetCategory.Chemical, "assay");

        var request = NewRequest();
        request.WorksheetLinks =
        [
            new CreateSpecificationWorksheetLinkRequest
            {
                WorksheetTemplateId = template.Id,
                AnalysisType = SpecificationAnalysisType.Chemical
            }
        ];
        request.Characteristics =
        [
            new CreateSpecificationCharacteristicRequest
            {
                TestName = "Dissolution",
                AcceptanceCriteria = "NLT 75%",
                SourceWorksheetTemplateId = template.Id,
                SourceFieldKey = "not_on_this_template",
                DisplayOrder = 1
            }
        ];

        var result = await harness.Specifications.CreateSpecification(request, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("QcSpecification.CharacteristicFieldKeyNotFound", result.Error.Code);
    }

    /// <summary>
    /// Criterion 2, resolution half — "current Effective version" means exactly that. A link
    /// pointing at a superseded version validates against the successor now in force, so a
    /// field added in the new version is accepted and one dropped from it is not.
    /// </summary>
    [Fact]
    public async Task Field_keys_resolve_against_the_current_effective_version_of_the_chain()
    {
        using var harness = new QcWorksheetTestContext();

        var v1 = await harness.SeedEffectiveTemplate("WS/CHEM", WorksheetCategory.Chemical, "assay", "retired_field");
        v1.Status = QcDocumentStatus.Superseded;

        var v2 = await harness.SeedEffectiveTemplate("WS/CHEM", WorksheetCategory.Chemical, "assay", "new_field");
        v2.Version = 2;
        v2.SupersedesId = v1.Id;
        await harness.Db.SaveChangesAsync();

        var request = NewRequest();
        request.WorksheetLinks =
        [
            // The specification still points at v1, the version it was authored against.
            new CreateSpecificationWorksheetLinkRequest
            {
                WorksheetTemplateId = v1.Id,
                AnalysisType = SpecificationAnalysisType.Chemical
            }
        ];
        request.Characteristics =
        [
            new CreateSpecificationCharacteristicRequest
            {
                TestName = "New test",
                AcceptanceCriteria = "Complies",
                SourceWorksheetTemplateId = v1.Id,
                SourceFieldKey = "new_field",
                DisplayOrder = 1
            }
        ];

        // A field that exists only on v2 is accepted, because v2 is what is in force.
        Assert.True((await harness.Specifications.CreateSpecification(request, Guid.NewGuid())).IsSuccess);

        // A field that existed only on the superseded v1 is not.
        request.Code = "QCD/SPEC/RM/002";
        request.Characteristics[0].SourceFieldKey = "retired_field";

        var stale = await harness.Specifications.CreateSpecification(request, Guid.NewGuid());
        Assert.False(stale.IsSuccess);
        Assert.Equal("QcSpecification.CharacteristicFieldKeyNotFound", stale.Error.Code);
    }

    /// <summary>
    /// Criterion 2, supporting endpoint — available-fields returns the linked templates'
    /// in-force fields, which is what the SourceFieldKey dropdown is built from.
    /// </summary>
    [Fact]
    public async Task Available_fields_returns_the_linked_templates_fields()
    {
        using var harness = new QcWorksheetTestContext();
        var chemical = await harness.SeedEffectiveTemplate("WS/CHEM", WorksheetCategory.Chemical, "assay", "water");
        var microbial = await harness.SeedEffectiveTemplate("WS/MICRO", WorksheetCategory.Microbial, "tamc");
        var unlinked = await harness.SeedEffectiveTemplate("WS/OTHER", WorksheetCategory.Chemical, "should_not_appear");

        var request = NewRequest();
        request.WorksheetLinks =
        [
            new CreateSpecificationWorksheetLinkRequest
            {
                WorksheetTemplateId = chemical.Id,
                AnalysisType = SpecificationAnalysisType.Chemical
            },
            new CreateSpecificationWorksheetLinkRequest
            {
                WorksheetTemplateId = microbial.Id,
                AnalysisType = SpecificationAnalysisType.Microbial
            }
        ];

        var created = await harness.Specifications.CreateSpecification(request, Guid.NewGuid());
        Assert.True(created.IsSuccess);

        var fields = await harness.Specifications.GetAvailableFields(created.Value.Id);

        Assert.True(fields.IsSuccess);
        Assert.Equal(3, fields.Value.Count);
        Assert.Contains(fields.Value, field => field.FieldKey == "assay");
        Assert.Contains(fields.Value, field => field.FieldKey == "water");
        Assert.Contains(fields.Value, field => field.FieldKey == "tamc");
        Assert.DoesNotContain(fields.Value, field => field.FieldKey == "should_not_appear");
        Assert.DoesNotContain(fields.Value, field => field.WorksheetTemplateId == unlinked.Id);
    }

    // -----------------------------------------------------------------------
    // Criterion 3 — same field, two tiers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Criterion 3 — one EM test gets two rows off the same worksheet field, separated by
    /// sampling point group and carrying different Action limits. This must not be mistaken
    /// for a duplicate.
    /// </summary>
    [Fact]
    public async Task Same_source_field_saves_twice_under_different_sampling_point_groups()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate(
            "WS/EM", WorksheetCategory.Microbial, "airborne_viables");

        var generalRooms = await harness.SeedSamplingPointGroup("General Rooms");
        var dispensingBooth = await harness.SeedSamplingPointGroup("Dispensing Booth");

        var request = NewRequest(SpecificationAppliesTo.RoutineEnvironmental);
        request.WorksheetLinks =
        [
            new CreateSpecificationWorksheetLinkRequest
            {
                WorksheetTemplateId = template.Id,
                AnalysisType = SpecificationAnalysisType.Microbial
            }
        ];
        request.Characteristics =
        [
            new CreateSpecificationCharacteristicRequest
            {
                TestName = "Airborne Viables",
                AcceptanceCriteria = "Within limits",
                AlertLimit = "80 CFU/4Hrs",
                ActionLimit = "NMT 100 CFU/4Hrs",
                SamplingPointGroupId = generalRooms.Id,
                SourceWorksheetTemplateId = template.Id,
                SourceFieldKey = "airborne_viables",
                DisplayOrder = 1
            },
            new CreateSpecificationCharacteristicRequest
            {
                TestName = "Airborne Viables",
                AcceptanceCriteria = "Within limits",
                AlertLimit = "3 CFU/4Hrs",
                ActionLimit = "NMT 5 CFU/4Hrs",
                SamplingPointGroupId = dispensingBooth.Id,
                SourceWorksheetTemplateId = template.Id,
                SourceFieldKey = "airborne_viables",
                DisplayOrder = 2
            }
        ];

        var result = await harness.Specifications.CreateSpecification(request, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Characteristics.Count);

        // Both rows kept their own tier, and both resolved their group for display.
        var rows = result.Value.Characteristics.OrderBy(row => row.DisplayOrder).ToList();
        Assert.Equal("NMT 100 CFU/4Hrs", rows[0].ActionLimit);
        Assert.Equal("NMT 5 CFU/4Hrs", rows[1].ActionLimit);
        Assert.Equal("General Rooms", rows[0].SamplingPointGroup.Name);
        Assert.Equal("Dispensing Booth", rows[1].SamplingPointGroup.Name);
        Assert.Equal(rows[0].SourceFieldKey, rows[1].SourceFieldKey);
    }

    // -----------------------------------------------------------------------
    // Criterion 4 — SamplingPointGroup is dropdown-only
    // -----------------------------------------------------------------------

    /// <summary>
    /// Criterion 4 — the only way to set a characteristic's sampling point group is by the id
    /// of an existing group. There is no name/free-text property on the request to type into
    /// (asserted by reflection, so adding one later fails this test), and an id that resolves
    /// to nothing is refused rather than stored.
    /// </summary>
    [Fact]
    public async Task Sampling_point_group_can_only_be_set_by_id_of_an_existing_group()
    {
        // No free-text path exists on the request contract.
        var groupProperties = typeof(CreateSpecificationCharacteristicRequest)
            .GetProperties()
            .Where(property => property.Name.Contains("SamplingPointGroup", StringComparison.Ordinal))
            .ToList();

        Assert.Equal(
            ["SamplingPointGroupId"],
            groupProperties.Select(property => property.Name).ToArray());
        Assert.Equal(typeof(Guid?), groupProperties[0].PropertyType);

        // And an id that does not resolve is rejected, not silently persisted as null.
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("WS/EM", WorksheetCategory.Microbial, "tamc");

        var request = NewRequest(SpecificationAppliesTo.RoutineEnvironmental);
        request.WorksheetLinks =
        [
            new CreateSpecificationWorksheetLinkRequest
            {
                WorksheetTemplateId = template.Id,
                AnalysisType = SpecificationAnalysisType.Microbial
            }
        ];
        request.Characteristics =
        [
            new CreateSpecificationCharacteristicRequest
            {
                TestName = "TAMC",
                AcceptanceCriteria = "NMT 100 CFU/g",
                SamplingPointGroupId = Guid.NewGuid(),
                SourceWorksheetTemplateId = template.Id,
                SourceFieldKey = "tamc",
                DisplayOrder = 1
            }
        ];

        var result = await harness.Specifications.CreateSpecification(request, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("QcSpecification.SamplingPointGroupNotFound", result.Error.Code);
    }

    // -----------------------------------------------------------------------
    // Criterion 5 — Stage required for Product, forbidden otherwise
    // -----------------------------------------------------------------------

    /// <summary>Criterion 5, first half — a Product specification without a Stage is rejected.</summary>
    [Fact]
    public async Task Product_specification_without_a_stage_is_rejected()
    {
        using var harness = new QcWorksheetTestContext();

        var result = await harness.Specifications.CreateSpecification(
            NewRequest(SpecificationAppliesTo.Product, stage: null), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("QcSpecification.StageRequired", result.Error.Code);
    }

    /// <summary>
    /// Criterion 5, second half — Stage is Product-only, not merely optional elsewhere. Every
    /// non-Product value is checked, so this cannot pass by only guarding one of them.
    /// </summary>
    [Theory]
    [InlineData(SpecificationAppliesTo.RawMaterial)]
    [InlineData(SpecificationAppliesTo.PackagingMaterial)]
    [InlineData(SpecificationAppliesTo.RoutineWater)]
    [InlineData(SpecificationAppliesTo.RoutineEnvironmental)]
    public async Task Stage_on_a_non_product_specification_is_rejected(SpecificationAppliesTo appliesTo)
    {
        using var harness = new QcWorksheetTestContext();

        var result = await harness.Specifications.CreateSpecification(
            NewRequest(appliesTo, stage: SpecificationStage.Finished), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("QcSpecification.StageNotApplicable", result.Error.Code);
    }

    /// <summary>Criterion 5 — the two legitimate shapes still save.</summary>
    [Theory]
    [InlineData(SpecificationStage.Intermediate)]
    [InlineData(SpecificationStage.Bulk)]
    [InlineData(SpecificationStage.Finished)]
    public async Task Product_specification_with_a_stage_saves(SpecificationStage stage)
    {
        using var harness = new QcWorksheetTestContext();

        var result = await harness.Specifications.CreateSpecification(
            NewRequest(SpecificationAppliesTo.Product, stage), Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(stage, result.Value.Stage);
    }

    /// <summary>Criterion 5 — a non-Product specification saves when Stage is left unset.</summary>
    [Fact]
    public async Task Non_product_specification_without_a_stage_saves()
    {
        using var harness = new QcWorksheetTestContext();

        var result = await harness.Specifications.CreateSpecification(
            NewRequest(SpecificationAppliesTo.RawMaterial), Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Stage);
    }

    /// <summary>
    /// Criterion 5 — the rule holds on edit too, not just on create. An existing valid
    /// Product specification cannot be edited into a RawMaterial one that keeps its Stage.
    /// </summary>
    [Fact]
    public async Task Stage_rule_is_enforced_on_update_as_well_as_create()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = Guid.NewGuid();

        var created = await harness.Specifications.CreateSpecification(
            NewRequest(SpecificationAppliesTo.Product, SpecificationStage.Finished), userId);
        Assert.True(created.IsSuccess);

        var update = new UpdateSpecificationRequest
        {
            Code = "QCD/SPEC/RM/001",
            Name = "Ascorbic Acid",
            AppliesTo = SpecificationAppliesTo.RawMaterial,
            Stage = SpecificationStage.Finished,
            RetestPolicy = QcRetestPolicy.SameSample
        };

        var result = await harness.Specifications.UpdateSpecification(created.Value.Id, update, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcSpecification.StageNotApplicable", result.Error.Code);
    }

    // -----------------------------------------------------------------------
    // Criterion 6 — edit-triggers-versioning
    // -----------------------------------------------------------------------

    /// <summary>
    /// Criterion 6 — Milestone 1's criterion 1 run against Specification, confirming the
    /// shared lifecycle was not implemented differently for this entity. An Effective
    /// specification refuses edits; creating version 2 leaves version 1 Effective.
    /// </summary>
    [Fact]
    public async Task Effective_specification_rejects_edits_and_new_version_leaves_v1_effective()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.Specification);
        var userId = harness.Approver.Id;

        var template = await harness.SeedEffectiveTemplate("WS/CHEM", WorksheetCategory.Chemical, "assay");

        var request = NewRequest();
        request.WorksheetLinks =
        [
            new CreateSpecificationWorksheetLinkRequest
            {
                WorksheetTemplateId = template.Id,
                AnalysisType = SpecificationAnalysisType.Chemical
            }
        ];
        request.Characteristics =
        [
            new CreateSpecificationCharacteristicRequest
            {
                TestName = "Assay",
                AcceptanceCriteria = "95.0-105.0%",
                SourceWorksheetTemplateId = template.Id,
                SourceFieldKey = "assay",
                DisplayOrder = 1
            }
        ];

        var created = await harness.Specifications.CreateSpecification(request, userId);
        Assert.True(created.IsSuccess);
        var v1Id = created.Value.Id;

        Assert.True((await harness.Specifications.SubmitForReview(v1Id, userId)).IsSuccess);

        var approved = await harness.Specifications.Approve(
            v1Id,
            new QcApprovalRequest
            {
                Password = QcWorksheetTestContext.CorrectPassword,
                Comments = "Approved by"
            },
            userId,
            []);
        Assert.True(approved.IsSuccess);

        var effective = await harness.Specifications.MakeEffective(v1Id, userId);
        Assert.True(effective.IsSuccess);
        Assert.Equal(QcDocumentStatus.Effective, effective.Value.Status);

        // Editing an Effective record is refused server-side — the read-only UI is never
        // trusted on its own.
        var edit = await harness.Specifications.UpdateSpecification(v1Id, new UpdateSpecificationRequest
        {
            Code = "QCD/SPEC/RM/001",
            Name = "Renamed while effective",
            AppliesTo = SpecificationAppliesTo.RawMaterial,
            RetestPolicy = QcRetestPolicy.SameSample
        }, userId);

        Assert.False(edit.IsSuccess);
        Assert.Equal("QcWorksheet.NotEditable", edit.Error.Code);

        var v2 = await harness.Specifications.CreateNewVersion(v1Id, userId);
        Assert.True(v2.IsSuccess);
        Assert.Equal(2, v2.Value.Version);
        Assert.Equal(QcDocumentStatus.Draft, v2.Value.Status);
        Assert.Equal(v1Id, v2.Value.SupersedesId);

        // Version 1 stays in force until version 2 itself becomes Effective.
        var v1 = await harness.Specifications.GetSpecification(v1Id);
        Assert.Equal(QcDocumentStatus.Effective, v1.Value.Status);
        Assert.Equal(1, v1.Value.Version);

        // Content carried over, including both child collections.
        Assert.Single(v2.Value.WorksheetLinks);
        Assert.Single(v2.Value.Characteristics);
        Assert.Equal("assay", v2.Value.Characteristics[0].SourceFieldKey);
        Assert.Equal(QcRetestPolicy.SameSample, v2.Value.RetestPolicy);
    }

    /// <summary>Criterion 6 — Draft edits are free and never spawn a version.</summary>
    [Fact]
    public async Task Draft_specification_edits_do_not_create_versions()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = Guid.NewGuid();

        var created = await harness.Specifications.CreateSpecification(NewRequest(), userId);
        Assert.True(created.IsSuccess);

        for (var i = 0; i < 3; i++)
        {
            var edited = await harness.Specifications.UpdateSpecification(
                created.Value.Id,
                new UpdateSpecificationRequest
                {
                    Code = "QCD/SPEC/RM/001",
                    Name = $"Edit {i}",
                    AppliesTo = SpecificationAppliesTo.RawMaterial,
                    RetestPolicy = QcRetestPolicy.FreshResample
                },
                userId);

            Assert.True(edited.IsSuccess);
            Assert.Equal(1, edited.Value.Version);
            Assert.Equal(QcDocumentStatus.Draft, edited.Value.Status);
        }

        Assert.Equal(1, await harness.Db.QcSpecifications.CountAsync());
    }

    /// <summary>
    /// Criterion 6 — an edit while UnderReview returns the document to Draft, so a reviewer
    /// is never left evaluating a moving target.
    /// </summary>
    [Fact]
    public async Task Editing_an_under_review_specification_resets_it_to_draft()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.Specification);
        var userId = harness.Approver.Id;

        var created = await harness.Specifications.CreateSpecification(NewRequest(), userId);
        var submitted = await harness.Specifications.SubmitForReview(created.Value.Id, userId);
        Assert.True(submitted.IsSuccess);
        Assert.Equal(QcDocumentStatus.UnderReview, submitted.Value.Status);

        var edited = await harness.Specifications.UpdateSpecification(
            created.Value.Id,
            new UpdateSpecificationRequest
            {
                Code = "QCD/SPEC/RM/001",
                Name = "Edited under review",
                AppliesTo = SpecificationAppliesTo.RawMaterial,
                RetestPolicy = QcRetestPolicy.SameSample
            },
            userId);

        Assert.True(edited.IsSuccess);
        Assert.Equal(QcDocumentStatus.Draft, edited.Value.Status);
        Assert.False(edited.Value.Approved);
    }

    /// <summary>
    /// Criterion 6, signature half — approving a Specification goes through the same
    /// centralized QcApproval table and re-auth gate as Milestone 1's entities, rather than a
    /// second mechanism invented for this entity.
    /// </summary>
    [Fact]
    public async Task Approving_a_specification_records_a_reauthenticated_qc_approval()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.Specification);
        var userId = harness.Approver.Id;

        var created = await harness.Specifications.CreateSpecification(NewRequest(), userId);
        await harness.Specifications.SubmitForReview(created.Value.Id, userId);

        // A wrong password is refused even though the session is otherwise valid.
        var wrong = await harness.Specifications.Approve(
            created.Value.Id,
            new QcApprovalRequest { Password = "not-my-password", Comments = "Approved by" },
            userId,
            []);

        Assert.False(wrong.IsSuccess);
        Assert.Equal(QcDocumentStatus.UnderReview,
            (await harness.Specifications.GetSpecification(created.Value.Id)).Value.Status);

        var approved = await harness.Specifications.Approve(
            created.Value.Id,
            new QcApprovalRequest
            {
                Password = QcWorksheetTestContext.CorrectPassword,
                Comments = "Approved by"
            },
            userId,
            []);

        Assert.True(approved.IsSuccess);
        Assert.Equal(QcDocumentStatus.Approved, approved.Value.Status);

        var signature = await harness.Db.QcApprovals.SingleAsync(row =>
            row.EntityType == QcApprovalEntityTypes.Specification
            && row.EntityId == created.Value.Id);

        Assert.NotNull(signature.ReauthConfirmedAt);
        Assert.Equal(userId, signature.ApprovedById);
        Assert.NotNull(signature.ApprovalTime);

        // It lands in the one shared queue, not a Specification-only one.
        var pending = await harness.Approvals.GetApprovalsForEntity(
            QcApprovalEntityTypes.Specification, created.Value.Id);
        Assert.True(pending.IsSuccess);
        Assert.Single(pending.Value);
    }

    /// <summary>
    /// Criterion 6, auto-approval half — a Specification inherits Milestone 1's refusal to
    /// self-approve when no chain is configured, rather than the engine's usual silent
    /// auto-approve.
    /// </summary>
    [Fact]
    public async Task Submitting_without_a_configured_chain_fails_loudly()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = Guid.NewGuid();

        var created = await harness.Specifications.CreateSpecification(NewRequest(), userId);
        var submitted = await harness.Specifications.SubmitForReview(created.Value.Id, userId);

        Assert.False(submitted.IsSuccess);
        Assert.Equal("QcWorksheet.NoApprovalWorkflowConfigured", submitted.Error.Code);

        // And it certainly did not become Approved.
        var reread = await harness.Specifications.GetSpecification(created.Value.Id);
        Assert.Equal(QcDocumentStatus.Draft, reread.Value.Status);
        Assert.False(reread.Value.Approved);
    }

    // -----------------------------------------------------------------------
    // Criterion 7 — RetestPolicy is mandatory
    // -----------------------------------------------------------------------

    /// <summary>
    /// Criterion 7 — there is no default to silently fall back to. Both an omitted policy and
    /// a raw zero (the CLR default an enum would otherwise bind to) are refused.
    /// </summary>
    [Fact]
    public async Task Specification_without_a_retest_policy_is_rejected()
    {
        using var harness = new QcWorksheetTestContext();

        var omitted = await harness.Specifications.CreateSpecification(
            NewRequest(retestPolicy: null), Guid.NewGuid());

        Assert.False(omitted.IsSuccess);
        Assert.Equal("QcSpecification.RetestPolicyRequired", omitted.Error.Code);

        // The enum deliberately has no member at 0, so a payload carrying the CLR default is
        // caught rather than read as SameSample.
        var zero = await harness.Specifications.CreateSpecification(
            NewRequest(retestPolicy: default(QcRetestPolicy)), Guid.NewGuid());

        Assert.False(zero.IsSuccess);
        Assert.Equal("QcSpecification.RetestPolicyRequired", zero.Error.Code);
        Assert.False(await harness.Db.QcSpecifications.AnyAsync());
    }

    /// <summary>Criterion 7 — both declared policies save and round-trip.</summary>
    [Theory]
    [InlineData(QcRetestPolicy.SameSample)]
    [InlineData(QcRetestPolicy.FreshResample)]
    public async Task Declared_retest_policy_round_trips(QcRetestPolicy policy)
    {
        using var harness = new QcWorksheetTestContext();

        var created = await harness.Specifications.CreateSpecification(
            NewRequest(retestPolicy: policy), Guid.NewGuid());

        Assert.True(created.IsSuccess);
        Assert.Equal(policy, created.Value.RetestPolicy);

        var reread = await harness.Specifications.GetSpecification(created.Value.Id);
        Assert.Equal(policy, reread.Value.RetestPolicy);
    }
}
