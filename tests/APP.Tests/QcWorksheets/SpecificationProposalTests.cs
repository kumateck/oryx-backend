using System.Reflection;
using API.Controllers;
using APP.Repository.QcWorksheets;
using APP.Tests.QcWorksheets.WorksheetImport;
using APP.Utils;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// Build brief 08 — Specification proposals from worksheet import. Runs against the real
/// <see cref="SpecificationRepository"/>, so every applied plan passes the M2 validation a
/// hand-authored Specification does.
/// </summary>
public class SpecificationProposalTests(ITestOutputHelper output)
{
    private const string EmField = "airborne_viables";
    private const string WaterField = "tamc_count";

    private static SpecificationProposalRepository Proposals(QcWorksheetTestContext harness) =>
        new(harness.Db, harness.Mapper, harness.Specifications);

    private static SpecificationCharacteristicProposal Tier(string group, string limit, string field = EmField) => new()
    {
        TestName = "Airborne viables",
        AcceptanceCriteria = limit,
        ActionLimit = limit,
        SourceFieldKey = field,
        GroupName = group
    };

    private static SamplingPointGroupProposal Group(string name, string limit, params string[] codes) => new()
    {
        Name = name,
        AcceptanceCriteria = limit,
        PrintedPoints = name,
        PointCodes = codes.ToList()
    };

    private static async Task<Guid> Store(
        QcWorksheetTestContext harness,
        ArdFamily family,
        Guid templateId,
        List<SpecificationCharacteristicProposal> characteristics,
        List<SamplingPointGroupProposal> groups = null,
        string file = "sheet.docx",
        string productName = null,
        string specificationCode = null)
    {
        var result = await Proposals(harness).CreateProposalSet(new CreateSpecificationProposalSetRequest
        {
            Family = family,
            SourceFileName = file,
            WorksheetTemplateId = templateId,
            ProductName = productName,
            SpecificationCode = specificationCode,
            SpecificationProposals = characteristics,
            SamplingPointGroupProposals = groups ?? [],
            SamplingPointCodes = (groups ?? []).SelectMany(group => group.PointCodes).ToList()
        }, Guid.NewGuid());

        Assert.True(result.IsSuccess, result.Error?.Description);
        return result.Value.Id;
    }

    private static async Task<SpecificationDraftPlan> Draft(QcWorksheetTestContext harness, params Guid[] ids)
    {
        var result = await Proposals(harness).BuildDraftPlan(new SpecificationProposalDraftRequest { ProposalSetIds = ids.ToList() });
        Assert.True(result.IsSuccess, result.Error?.Description);
        return result.Value;
    }

    private static Task<SHARED.Result<SpecificationProposalApplyResult>> Apply(
        QcWorksheetTestContext harness, SpecificationDraftPlan plan, params Guid[] ids) =>
        Proposals(harness).Apply(new SpecificationProposalApplyRequest { ProposalSetIds = ids.ToList(), Plan = plan }, Guid.NewGuid());

    private static async Task SeedPoints(QcWorksheetTestContext harness, SamplingPointType type, params string[] codes)
    {
        foreach (var code in codes)
            await harness.SeedSamplingPoint(code, type);
    }

    // -----------------------------------------------------------------------
    // Store
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Media_and_empty_sets_are_never_stored()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("WS/MEDIA", WorksheetCategory.Microbial, "growth");
        var repository = Proposals(harness);

        var media = await repository.CreateProposalSet(new CreateSpecificationProposalSetRequest
        {
            Family = ArdFamily.CultureMedia, SourceFileName = "media.docx", WorksheetTemplateId = template.Id,
            SpecificationProposals = [Tier(null, "Growth", "growth")]
        }, Guid.NewGuid());
        Assert.Equal("QcSpecificationProposal.FamilyNotSupported", media.Error.Code);

        var empty = await repository.CreateProposalSet(new CreateSpecificationProposalSetRequest
        {
            Family = ArdFamily.EnvironmentalMonitoring, SourceFileName = "em.docx", WorksheetTemplateId = template.Id
        }, Guid.NewGuid());
        Assert.Equal("QcSpecificationProposal.NoCharacteristics", empty.Error.Code);

        Assert.False(await harness.Db.QcSpecificationProposalSets.AnyAsync());
    }

    // -----------------------------------------------------------------------
    // Product
    // -----------------------------------------------------------------------

    [Fact]
    public async Task A_product_ard_drafts_a_microbial_only_finished_specification()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("WS/FP/MICRO", WorksheetCategory.Microbial, "tamc", "tymc", "ecoli");

        var id = await Store(harness, ArdFamily.ProductMicro, template.Id,
        [
            new() { TestName = "TAMC", AcceptanceCriteria = "NMT 1000 cfu/g", SourceFieldKey = "tamc" },
            new() { TestName = "TYMC", AcceptanceCriteria = "NMT 100 cfu/g", SourceFieldKey = "tymc" },
            new() { TestName = "E. coli", AcceptanceCriteria = "Absence of E.coli", SourceFieldKey = "ecoli" },
            new() { TestName = "Salmonella", AcceptanceCriteria = "Absence", SourceFieldKey = "not_on_template" }
        ], productName: "Paracetamol Syrup", specificationCode: "SPEC/FP/001");

        var plan = await Draft(harness, id);

        Assert.Equal(("SPEC/FP/001", "Paracetamol Syrup"), (plan.Code, plan.Name));
        Assert.Equal(SpecificationAppliesTo.Product, plan.AppliesTo);
        Assert.Equal(SpecificationStage.Finished, plan.Stage);
        Assert.Null(plan.RetestPolicy);
        var link = Assert.Single(plan.WorksheetLinks);
        Assert.Equal((template.Id, SpecificationAnalysisType.Microbial), (link.WorksheetTemplateId, link.AnalysisType!.Value));
        Assert.Equal(["tamc", "tymc", "ecoli"], plan.Characteristics.Select(row => row.SourceFieldKey));
        Assert.All(plan.Characteristics, row =>
        {
            Assert.Equal("MICROBIAL", row.GroupName);
            Assert.True(row.IncludeOnCoa);
            Assert.Null(row.SamplingPointGroupName);
        });
        var warning = Assert.Single(plan.Warnings);
        Assert.Equal(SpecificationDraftPlanWarningCodes.FieldNotOnTemplate, warning.Code);
        Assert.Empty(plan.Groups);

        plan.RetestPolicy = QcRetestPolicy.FreshResample;
        var applied = await Apply(harness, plan, id);
        Assert.True(applied.IsSuccess, applied.Error?.Description);

        var specification = await harness.Db.QcSpecifications.Include(item => item.Characteristics)
            .SingleAsync(item => item.Id == applied.Value.SpecificationId);
        Assert.Equal(QcDocumentStatus.Draft, specification.Status);
        Assert.False(specification.Approved);
        Assert.Equal(3, specification.Characteristics.Count);

        var set = await harness.Db.QcSpecificationProposalSets.SingleAsync();
        Assert.Equal((SpecificationProposalStatus.Applied, specification.Id), (set.Status, set.AppliedSpecificationId));
    }

    [Fact]
    public async Task A_template_that_is_not_effective_is_only_a_warning()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("WS/FP/DRAFT", WorksheetCategory.Microbial, "tamc");
        var tracked = await harness.Db.QcWorksheetTemplates.SingleAsync(item => item.Id == template.Id);
        tracked.Status = QcDocumentStatus.Draft;
        await harness.Db.SaveChangesAsync();

        var id = await Store(harness, ArdFamily.ProductMicro, template.Id,
            [new() { TestName = "TAMC", AcceptanceCriteria = "NMT 1000 cfu/g", SourceFieldKey = "tamc" }],
            productName: "Product", specificationCode: "SPEC/1");

        var plan = await Draft(harness, id);
        Assert.Equal(SpecificationDraftPlanWarningCodes.TemplateNotEffective, Assert.Single(plan.Warnings).Code);

        plan.RetestPolicy = QcRetestPolicy.SameSample;
        Assert.True((await Apply(harness, plan, id)).IsSuccess);
    }

    // -----------------------------------------------------------------------
    // Environmental monitoring
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Eight_em_area_sets_make_one_specification()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("EM-AIRBORNE-VIABLES", WorksheetCategory.Microbial, EmField);

        var ids = new List<Guid>();
        for (var area = 1; area <= 8; area++)
        {
            var rooms = new[] { $"A{area}-01", $"A{area}-02" };
            var booth = $"A{area}-DB";
            await SeedPoints(harness, SamplingPointType.Environmental, [.. rooms, booth]);

            ids.Add(await Store(harness, ArdFamily.EnvironmentalMonitoring, template.Id,
                [Tier("Rooms", "NMT 100 CFU/4Hrs"), Tier("Dispensing Booth", "NMT 5 CFU/4Hrs")],
                [Group("Rooms", "NMT 100 CFU/4Hrs", rooms), Group("Dispensing Booth", "NMT 5 CFU/4Hrs", booth)],
                file: $"area-{area}.docx"));
        }

        var plan = await Draft(harness, [.. ids]);

        Assert.Null(plan.TargetSpecificationId);
        Assert.Equal(SpecificationAppliesTo.RoutineEnvironmental, plan.AppliesTo);
        Assert.Null(plan.Stage);
        Assert.Empty(plan.Warnings);
        Assert.Equal(["Rooms", "Dispensing Booth"], plan.Characteristics.Select(row => row.SamplingPointGroupName));
        Assert.Equal(2, plan.Groups.Count);
        Assert.All(plan.Groups, group => Assert.True(group.IsNew));
        Assert.Equal(16, plan.Groups.Single(group => group.Name == "Rooms").PointCodes.Count);

        plan.Code = "SPEC/EM/001";
        plan.RetestPolicy = QcRetestPolicy.FreshResample;
        var applied = await Apply(harness, plan, [.. ids]);
        Assert.True(applied.IsSuccess, applied.Error?.Description);

        var specification = await harness.Db.QcSpecifications.Include(item => item.Characteristics)
            .Include(item => item.WorksheetLinks).SingleAsync();
        Assert.Equal(specification.Id, applied.Value.SpecificationId);
        Assert.Equal(SpecificationAnalysisType.Microbial, Assert.Single(specification.WorksheetLinks).AnalysisType);
        Assert.Equal(2, specification.Characteristics.Count);
        Assert.All(specification.Characteristics, row => Assert.NotNull(row.SamplingPointGroupId));

        var roomTier = await harness.Db.QcSamplingPointGroups.SingleAsync(group => group.Name == "Rooms");
        Assert.Equal(16, await harness.Db.QcSamplingPoints.CountAsync(point => point.SamplingPointGroupId == roomTier.Id));
        Assert.All(await harness.Db.QcSpecificationProposalSets.ToListAsync(),
            set => Assert.Equal(SpecificationProposalStatus.Applied, set.Status));
    }

    [Fact]
    public async Task A_later_em_upload_appends_its_new_tiers_to_the_draft_specification()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("EM-AIRBORNE-VIABLES", WorksheetCategory.Microbial, EmField);
        await SeedPoints(harness, SamplingPointType.Environmental, "R-1", "R-2", "LAF-1");

        var first = await Store(harness, ArdFamily.EnvironmentalMonitoring, template.Id,
            [Tier("Rooms", "NMT 100")], [Group("Rooms", "NMT 100", "R-1")]);
        var plan = await Draft(harness, first);
        plan.Code = "SPEC/EM/001";
        plan.RetestPolicy = QcRetestPolicy.FreshResample;
        var created = await Apply(harness, plan, first);
        Assert.True(created.IsSuccess, created.Error?.Description);

        var second = await Store(harness, ArdFamily.EnvironmentalMonitoring, template.Id,
            [Tier("Rooms", "NMT 100"), Tier("LAF Bench", "NMT 1")],
            [Group("Rooms", "NMT 100", "R-2"), Group("LAF Bench", "NMT 1", "LAF-1")]);

        var append = await Draft(harness, second);
        Assert.Equal(created.Value.SpecificationId, append.TargetSpecificationId);
        Assert.Equal("SPEC/EM/001", append.Code);
        Assert.Equal(["LAF Bench"], append.Characteristics.Select(row => row.SamplingPointGroupName));

        append.RetestPolicy = QcRetestPolicy.FreshResample;
        var applied = await Apply(harness, append, second);
        Assert.True(applied.IsSuccess, applied.Error?.Description);
        Assert.Equal(created.Value.SpecificationId, applied.Value.SpecificationId);

        // No-tracking: the context soft-deletes the replaced rows, which stay in the change
        // tracker and would otherwise be fixed up back into a tracked graph.
        var specification = await harness.Db.QcSpecifications.AsNoTracking()
            .Include(item => item.Characteristics).SingleAsync();
        Assert.Equal(2, specification.Characteristics.Count);
        Assert.Equal(QcDocumentStatus.Draft, specification.Status);

        // The later upload's room joined the existing tier.
        var rooms = await harness.Db.QcSamplingPointGroups.SingleAsync(group => group.Name == "Rooms");
        Assert.Equal(2, await harness.Db.QcSamplingPoints.CountAsync(point => point.SamplingPointGroupId == rooms.Id));
    }

    [Fact]
    public async Task An_effective_em_specification_only_warns_and_plans_a_new_one()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("EM-AIRBORNE-VIABLES", WorksheetCategory.Microbial, EmField);
        harness.Db.QcSpecifications.Add(new Specification
        {
            Id = Guid.NewGuid(), Code = "SPEC/EM/OLD", Name = "EM", AppliesTo = SpecificationAppliesTo.RoutineEnvironmental,
            Status = QcDocumentStatus.Effective, RetestPolicy = QcRetestPolicy.FreshResample, CreatedAt = DateTime.UtcNow
        });
        await harness.Db.SaveChangesAsync();

        var id = await Store(harness, ArdFamily.EnvironmentalMonitoring, template.Id,
            [Tier("Rooms", "NMT 100")], [Group("Rooms", "NMT 100")]);
        var plan = await Draft(harness, id);

        Assert.Null(plan.TargetSpecificationId);
        Assert.Equal(SpecificationDraftPlanWarningCodes.EffectiveEmSpecificationExists, Assert.Single(plan.Warnings).Code);
    }

    [Fact]
    public async Task A_tier_with_conflicting_limits_is_warned_and_blocks_apply_until_resolved()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("EM-AIRBORNE-VIABLES", WorksheetCategory.Microbial, EmField);

        var a = await Store(harness, ArdFamily.EnvironmentalMonitoring, template.Id, [Tier("Rooms", "NMT 100")], [Group("Rooms", "NMT 100")]);
        var b = await Store(harness, ArdFamily.EnvironmentalMonitoring, template.Id, [Tier("Rooms", "NMT 80")], [Group("Rooms", "NMT 80")]);

        var plan = await Draft(harness, a, b);
        Assert.Equal(SpecificationDraftPlanWarningCodes.TierConflict, Assert.Single(plan.Warnings).Code);
        Assert.Equal(2, plan.Characteristics.Count);

        plan.Code = "SPEC/EM/001";
        plan.RetestPolicy = QcRetestPolicy.FreshResample;
        var refused = await Apply(harness, plan, a, b);
        Assert.Equal("QcSpecificationProposal.TierConflict", refused.Error.Code);
        Assert.False(await harness.Db.QcSpecifications.AnyAsync());

        plan.Characteristics.RemoveAt(1);
        Assert.True((await Apply(harness, plan, a, b)).IsSuccess);
    }

    [Fact]
    public async Task A_suggested_alert_limit_on_one_sheet_is_not_a_tier_conflict()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("EM-AIRBORNE-VIABLES", WorksheetCategory.Microbial, EmField);

        var withAlert = Tier("Sampling Booth", "NMT 5 cfu/4Hrs");
        withAlert.AlertLimit = "NMT 3 cfu/4Hrs";
        var a = await Store(harness, ArdFamily.EnvironmentalMonitoring, template.Id, [Tier("Sampling Booth", "NMT 5 cfu/4Hrs")]);
        var b = await Store(harness, ArdFamily.EnvironmentalMonitoring, template.Id, [withAlert]);

        var plan = await Draft(harness, a, b);

        Assert.Empty(plan.Warnings);
        var row = Assert.Single(plan.Characteristics);
        Assert.Equal("NMT 3 cfu/4Hrs", row.AlertLimit);
    }

    [Fact]
    public async Task A_point_already_in_a_different_group_is_not_moved()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("EM-AIRBORNE-VIABLES", WorksheetCategory.Microbial, EmField);
        var other = await harness.SamplingPointGroups.CreateSamplingPointGroup(
            new CreateSamplingPointGroupRequest { Name = "Grade A" }, Guid.NewGuid());
        await harness.SeedSamplingPoint("R-1", SamplingPointType.Environmental, other.Value.Id);

        var id = await Store(harness, ArdFamily.EnvironmentalMonitoring, template.Id,
            [Tier("Rooms", "NMT 100")], [Group("Rooms", "NMT 100", "R-1")]);
        var plan = await Draft(harness, id);
        plan.Code = "SPEC/EM/001";
        plan.RetestPolicy = QcRetestPolicy.FreshResample;

        var result = await Apply(harness, plan, id);
        Assert.Equal("QcSpecificationProposal.SamplingPointGroupChange", result.Error.Code);
        Assert.Equal(other.Value.Id, (await harness.Db.QcSamplingPoints.SingleAsync()).SamplingPointGroupId);
        Assert.False(await harness.Db.QcSamplingPointGroups.AnyAsync(group => group.Name == "Rooms"));
    }

    [Fact]
    public async Task A_point_code_with_no_sampling_point_is_rejected()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("EM-AIRBORNE-VIABLES", WorksheetCategory.Microbial, EmField);
        var id = await Store(harness, ArdFamily.EnvironmentalMonitoring, template.Id,
            [Tier("Rooms", "NMT 100")], [Group("Rooms", "NMT 100", "MISSING")]);
        var plan = await Draft(harness, id);
        plan.Code = "SPEC/EM/001";
        plan.RetestPolicy = QcRetestPolicy.FreshResample;

        Assert.Equal("QcSpecificationProposal.SamplingPointNotFound", (await Apply(harness, plan, id)).Error.Code);
    }

    // -----------------------------------------------------------------------
    // Water
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Water_drafts_one_characteristic_per_tier()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("WS/PW", WorksheetCategory.Microbial, WaterField, "ecoli");
        await SeedPoints(harness, SamplingPointType.Water, "SP1", "SP2", "SP3", "SP4", "SP9", "SP10", "SP15", "NSP1");

        var id = await Store(harness, ArdFamily.PurifiedWater, template.Id,
        [
            Tier("Purified water – NMT 500 cfu/mL", "NMT 500 cfu/mL", WaterField),
            Tier("Purified water – NMT 100 cfu/mL", "NMT 100 cfu/mL", WaterField),
            Tier("Purified water – NMT 80 cfu/mL", "NMT 80 cfu/mL", WaterField),
            new() { TestName = "Absence of E. coli", AcceptanceCriteria = "Absent", SourceFieldKey = "ecoli" }
        ],
        [
            Group("Purified water – NMT 500 cfu/mL", "NMT 500 cfu/mL", "SP1", "SP2", "SP3"),
            Group("Purified water – NMT 100 cfu/mL", "NMT 100 cfu/mL", "SP4", "SP9"),
            Group("Purified water – NMT 80 cfu/mL", "NMT 80 cfu/mL", "SP10", "SP15", "NSP1")
        ]);

        var plan = await Draft(harness, id);
        Assert.Equal(SpecificationAppliesTo.RoutineWater, plan.AppliesTo);
        Assert.Equal(3, plan.Characteristics.Count(row => row.SamplingPointGroupName is not null));
        Assert.Equal(3, plan.Groups.Count);
        Assert.Empty(plan.Warnings);

        plan.Code = "SPEC/PW/001";
        plan.RetestPolicy = QcRetestPolicy.FreshResample;
        var applied = await Apply(harness, plan, id);
        Assert.True(applied.IsSuccess, applied.Error?.Description);
        Assert.Equal(4, await harness.Db.QcSpecificationCharacteristics.CountAsync());
        Assert.Equal(8, await harness.Db.QcSamplingPoints.CountAsync(point => point.SamplingPointGroupId != null));
    }

    // -----------------------------------------------------------------------
    // Apply guards
    // -----------------------------------------------------------------------

    [Fact]
    public async Task A_null_retest_policy_is_rejected_before_anything_is_written()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("EM-AIRBORNE-VIABLES", WorksheetCategory.Microbial, EmField);
        await SeedPoints(harness, SamplingPointType.Environmental, "R-1");
        var id = await Store(harness, ArdFamily.EnvironmentalMonitoring, template.Id,
            [Tier("Rooms", "NMT 100")], [Group("Rooms", "NMT 100", "R-1")]);

        var plan = await Draft(harness, id);
        plan.Code = "SPEC/EM/001";

        var result = await Apply(harness, plan, id);
        Assert.Equal("QcSpecification.RetestPolicyRequired", result.Error.Code);
        Assert.False(await harness.Db.QcSpecifications.AnyAsync());
        Assert.False(await harness.Db.QcSamplingPointGroups.AnyAsync());
        Assert.Equal(SpecificationProposalStatus.Pending, (await harness.Db.QcSpecificationProposalSets.SingleAsync()).Status);
    }

    [Fact]
    public async Task Applying_the_same_sets_twice_is_rejected()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("WS/FP", WorksheetCategory.Microbial, "tamc");
        var id = await Store(harness, ArdFamily.ProductMicro, template.Id,
            [new() { TestName = "TAMC", AcceptanceCriteria = "NMT 1000", SourceFieldKey = "tamc" }],
            productName: "Product", specificationCode: "SPEC/1");

        var plan = await Draft(harness, id);
        plan.RetestPolicy = QcRetestPolicy.SameSample;
        Assert.True((await Apply(harness, plan, id)).IsSuccess);

        var again = await Apply(harness, plan, id);
        Assert.Equal("QcSpecificationProposal.NotPending", again.Error.Code);
        Assert.Equal(1, await harness.Db.QcSpecifications.CountAsync());

        var redraft = await Proposals(harness).BuildDraftPlan(new SpecificationProposalDraftRequest { ProposalSetIds = [id] });
        Assert.Equal("QcSpecificationProposal.NotPending", redraft.Error.Code);
    }

    [Fact]
    public async Task Sets_of_different_families_cannot_share_a_plan()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("EM-AIRBORNE-VIABLES", WorksheetCategory.Microbial, EmField);
        var em = await Store(harness, ArdFamily.EnvironmentalMonitoring, template.Id, [Tier("Rooms", "NMT 100")]);
        var water = await Store(harness, ArdFamily.PurifiedWater, template.Id, [Tier("Tier", "NMT 100")]);

        var result = await Proposals(harness).BuildDraftPlan(new SpecificationProposalDraftRequest { ProposalSetIds = [em, water] });
        Assert.Equal("QcSpecificationProposal.MixedFamilies", result.Error.Code);
    }

    [Fact]
    public async Task Dismiss_requires_a_reason_and_settles_the_set()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedEffectiveTemplate("WS/FP", WorksheetCategory.Microbial, "tamc");
        var id = await Store(harness, ArdFamily.ProductMicro, template.Id,
            [new() { TestName = "TAMC", AcceptanceCriteria = "NMT 1000", SourceFieldKey = "tamc" }]);
        var repository = Proposals(harness);

        var blank = await repository.Dismiss(id, new SpecificationProposalDismissRequest { Reason = " " }, Guid.NewGuid());
        Assert.Equal("QcSpecificationProposal.DismissReasonRequired", blank.Error.Code);

        var dismissed = await repository.Dismiss(id, new SpecificationProposalDismissRequest { Reason = "Duplicate upload" }, Guid.NewGuid());
        Assert.True(dismissed.IsSuccess);
        Assert.Equal((SpecificationProposalStatus.Dismissed, "Duplicate upload"), (dismissed.Value.Status, dismissed.Value.DismissReason));

        var pending = await repository.GetProposalSets(SpecificationProposalStatus.Pending, null);
        Assert.Empty(pending.Value);
    }

    // -----------------------------------------------------------------------
    // Permissions
    // -----------------------------------------------------------------------

    [Fact]
    public void Every_proposal_endpoint_is_gated_on_exactly_the_expected_key()
    {
        var actual = typeof(QcSpecificationProposalController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .ToDictionary(
                method => method.Name,
                method => Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>().Select(attribute => attribute.Policy)));

        Assert.Equal(new Dictionary<string, string>
        {
            // Storing is the save step of the import action, so it reuses the import key.
            [nameof(QcSpecificationProposalController.CreateProposalSet)] = QcWorksheetPermissionKeys.CanImportQcWorksheetTemplates,
            [nameof(QcSpecificationProposalController.GetProposalSets)] = QcWorksheetPermissionKeys.CanViewQcSpecificationProposals,
            [nameof(QcSpecificationProposalController.GetProposalSet)] = QcWorksheetPermissionKeys.CanViewQcSpecificationProposals,
            [nameof(QcSpecificationProposalController.BuildDraftPlan)] = QcWorksheetPermissionKeys.CanApplyQcSpecificationProposals,
            [nameof(QcSpecificationProposalController.Apply)] = QcWorksheetPermissionKeys.CanApplyQcSpecificationProposals,
            [nameof(QcSpecificationProposalController.Dismiss)] = QcWorksheetPermissionKeys.CanDismissQcSpecificationProposals
        }, actual);
    }

    [Fact]
    public void The_proposal_keys_are_registered_under_their_own_submodule()
    {
        foreach (var key in new[]
                 {
                     QcWorksheetPermissionKeys.CanViewQcSpecificationProposals,
                     QcWorksheetPermissionKeys.CanApplyQcSpecificationProposals,
                     QcWorksheetPermissionKeys.CanDismissQcSpecificationProposals
                 })
        {
            var permission = Assert.Single(PermissionUtils.GeneratePermissions().Where(item => item.Key == key));
            Assert.Equal(QcWorksheetPermissionCatalog.SpecificationProposals, permission.SubModule);
        }
    }

    // -----------------------------------------------------------------------
    // Real corpus (QC_ARD_CORPUS_DIR)
    // -----------------------------------------------------------------------

    /// <summary>Stores a corpus file's proposals against a template seeded with its proposed field keys.</summary>
    private static async Task<Guid> StoreCorpus(QcWorksheetTestContext harness, CorpusFile file, Guid templateId)
    {
        var proposal = file.Proposal;
        var first = proposal.SpecificationProposals.FirstOrDefault();
        return await Store(harness, proposal.Family, templateId, proposal.SpecificationProposals,
            proposal.SamplingPointGroupProposals, Path.GetFileName(file.RelativePath),
            first?.ProductName, first?.SpecificationCode);
    }

    private static string[] FieldKeys(WorksheetImportProposal proposal) =>
        proposal.Template.Sections.SelectMany(section => section.Fields).Select(field => field.FieldKey).ToArray();

    [CorpusFact]
    public async Task Corpus_em_sheets_make_one_specification()
    {
        using var harness = new QcWorksheetTestContext();
        var sheets = ArdCorpus.Load().Where(file => file.Expected == ArdFamily.EnvironmentalMonitoring).ToList();
        Assert.Equal(8, sheets.Count);

        var carrier = sheets.Single(file => file.Proposal.Template is not null);
        var template = await harness.SeedEffectiveTemplate(carrier.Proposal.Template.Code, WorksheetCategory.Microbial, FieldKeys(carrier.Proposal));

        foreach (var code in sheets.SelectMany(file => file.Proposal.SamplingPointProposals).Select(point => point.Code)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
            await harness.SeedSamplingPoint(code, SamplingPointType.Environmental);

        var ids = new List<Guid>();
        foreach (var sheet in sheets)
            ids.Add(await StoreCorpus(harness, sheet, template.Id));

        var plan = await Draft(harness, [.. ids]);
        foreach (var warning in plan.Warnings)
            output.WriteLine($"{warning.Code}: {warning.Message}");
        foreach (var row in plan.Characteristics)
            output.WriteLine($"{row.SamplingPointGroupName}: {row.ActionLimit}");

        Assert.DoesNotContain(plan.Warnings, warning => warning.Code == SpecificationDraftPlanWarningCodes.FieldNotOnTemplate);
        Assert.DoesNotContain(plan.Warnings, warning => warning.Code == SpecificationDraftPlanWarningCodes.TierConflict);
        Assert.All(plan.Characteristics, row => Assert.NotNull(row.SamplingPointGroupName));

        // Resolve any conflict the way a reviewer would — keep the first row per tier — then apply.
        plan.Characteristics = plan.Characteristics
            .GroupBy(row => row.SamplingPointGroupName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        plan.Code = "SPEC/EM/001";
        plan.RetestPolicy = QcRetestPolicy.FreshResample;

        var applied = await Apply(harness, plan, [.. ids]);
        Assert.True(applied.IsSuccess, applied.Error?.Description);
        Assert.Equal(1, await harness.Db.QcSpecifications.CountAsync());
        Assert.Equal(plan.Groups.Count, await harness.Db.QcSamplingPointGroups.CountAsync());
    }

    [CorpusFact]
    public async Task Corpus_water_sheet_drafts_three_tiers()
    {
        using var harness = new QcWorksheetTestContext();
        var water = ArdCorpus.Load().Where(file => file.Expected == ArdFamily.PurifiedWater).ToList();
        var sheet = water.Single(file => file.Proposal.SamplingPointGroupProposals.Count == 3);
        var template = await harness.SeedEffectiveTemplate(sheet.Proposal.Template.Code, WorksheetCategory.Microbial, FieldKeys(sheet.Proposal));
        foreach (var point in sheet.Proposal.SamplingPointProposals.DistinctBy(point => point.Code))
            await harness.SeedSamplingPoint(point.Code);

        var id = await StoreCorpus(harness, sheet, template.Id);
        var plan = await Draft(harness, id);
        foreach (var row in plan.Characteristics)
            output.WriteLine($"{row.SamplingPointGroupName ?? "(no tier)"}: {row.TestName} {row.AcceptanceCriteria}");

        Assert.Equal(SpecificationAppliesTo.RoutineWater, plan.AppliesTo);
        Assert.Equal(3, plan.Characteristics.Count(row => row.SamplingPointGroupName is not null));
        Assert.DoesNotContain(plan.Warnings, warning => warning.Code == SpecificationDraftPlanWarningCodes.TierConflict);

        plan.Code = "SPEC/PW/001";
        plan.RetestPolicy = QcRetestPolicy.FreshResample;
        var applied = await Apply(harness, plan, id);
        Assert.True(applied.IsSuccess, applied.Error?.Description);
    }

    [CorpusFact]
    public async Task Corpus_product_sheets_draft_microbial_finished_specifications()
    {
        var products = ArdCorpus.Load()
            .Where(file => file.Expected == ArdFamily.ProductMicro && file.Proposal.SpecificationProposals.Count > 0)
            .ToList();
        Assert.NotEmpty(products);

        foreach (var product in products)
        {
            using var harness = new QcWorksheetTestContext();
            var template = await harness.SeedEffectiveTemplate(product.Proposal.Template.Code, WorksheetCategory.Microbial, FieldKeys(product.Proposal));
            var id = await StoreCorpus(harness, product, template.Id);
            var plan = await Draft(harness, id);
            output.WriteLine($"{product.RelativePath}: {plan.Characteristics.Count} characteristics, {plan.Warnings.Count} warnings");

            Assert.Equal((SpecificationAppliesTo.Product, SpecificationStage.Finished), (plan.AppliesTo!.Value, plan.Stage!.Value));
            Assert.NotEmpty(plan.Characteristics);
            Assert.All(plan.Characteristics, row => Assert.Equal("MICROBIAL", row.GroupName));

            plan.Code ??= "SPEC/FP/TEST";
            plan.Name ??= "Product";
            plan.RetestPolicy = QcRetestPolicy.FreshResample;
            var applied = await Apply(harness, plan, id);
            Assert.True(applied.IsSuccess, $"{product.RelativePath}: {applied.Error?.Description}");
        }
    }
}
