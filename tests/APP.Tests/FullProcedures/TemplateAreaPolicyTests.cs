using APP.Services.FullProcedures;
using APP.Utils;
using DOMAIN.Entities.FullProcedures;
using Xunit;

namespace APP.Tests.FullProcedures;

public class TemplateAreaPolicyTests
{
    [Fact]
    public void Template_services_reject_cross_purpose_subjects_and_unsupported_kinds()
    {
        var area = new TemplateArea
        {
            Purposes = [new TemplateAreaPurpose { PurposeId = "hr" },
                new TemplateAreaPurpose { PurposeId = "production-procedure" }],
            SubjectTypes = [new TemplateAreaSubjectType { SubjectTypeId = "employee" },
                new TemplateAreaSubjectType { SubjectTypeId = "product" }],
        };

        Assert.True(TemplateAreaCatalogProvider.AllowsTemplateContext(
            area, "hr", "employee", TemplateDefinitionKind.Workflow));
        Assert.False(TemplateAreaCatalogProvider.AllowsTemplateContext(
            area, "hr", "product", TemplateDefinitionKind.Workflow));
        Assert.False(TemplateAreaCatalogProvider.AllowsTemplateContext(
            area, "hr", "employee", TemplateDefinitionKind.Activity));
        Assert.True(TemplateAreaCatalogProvider.AllowsTemplateContext(
            area, "production-procedure", "product", TemplateDefinitionKind.Activity));
    }

    private static readonly Guid OwnerGroupId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherOwnerGroupId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid AreaId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherAreaId = Guid.Parse("20000000-0000-0000-0000-000000000002");

    [Fact]
    public void Validates_company_defined_area_against_reviewed_server_catalog()
    {
        var result = TemplateAreaPolicy.ValidateDraft(ValidDraft(), [], Catalog());

        Assert.Empty(result);
    }

    [Fact]
    public void Rejects_duplicate_name_unknown_owner_and_unregistered_effect()
    {
        var draft = ValidDraft() with
        {
            Name = " environmental monitoring ",
            OwnerGroupId = OtherOwnerGroupId,
            CapabilityIds = ["script-execution"],
        };

        var result = TemplateAreaPolicy.ValidateDraft(draft, [ValidArea()], Catalog());

        Assert.Contains(TemplateAreaBlocker.DuplicateName, result);
        Assert.Contains(TemplateAreaBlocker.UnknownOwnerGroup, result);
        Assert.Contains(TemplateAreaBlocker.UnknownCapability, result);
        Assert.Contains(TemplateAreaBlocker.PurposeCapabilityMismatch, result);
    }

    [Fact]
    public void Rejects_context_outside_area_purpose_and_capability_scope()
    {
        var context = new TemplateDraftContext(
            AreaId,
            "environmental-monitoring",
            "product-batch",
            TemplateDefinitionKind.Workflow,
            ["controlled-stock-request"]
        );

        var result = TemplateAreaPolicy.ValidateContext(context, [ValidArea()], Catalog());

        Assert.Contains(TemplateAreaBlocker.SubjectNotInArea, result);
        Assert.Contains(TemplateAreaBlocker.PurposeSubjectMismatch, result);
        Assert.Contains(TemplateAreaBlocker.KindNotAllowed, result);
        Assert.Contains(TemplateAreaBlocker.CapabilityNotInArea, result);
        Assert.Contains(TemplateAreaBlocker.CapabilityNotInPurpose, result);
    }

    [Fact]
    public void Capability_key_without_area_assignment_is_denied()
    {
        var actor = Scope(
            Set(FullProcedurePermissionKeys.CanReviewTemplateRevision),
            Set(OtherAreaId),
            Set(OwnerGroupId)
        );

        var result = TemplateAreaAuthorization.Authorize(
            TemplateAreaOperation.ReviewRevision,
            actor,
            AreaId
        );

        Assert.Equal([TemplateAreaAccessDenial.AreaNotAssigned], result);
    }

    [Fact]
    public void Area_manager_cannot_publish_or_read_responses_by_implication()
    {
        var actor = Scope(
            Set(FullProcedurePermissionKeys.CanManageTemplateAreas),
            Set(AreaId),
            Set(OwnerGroupId)
        );

        Assert.Contains(
            TemplateAreaAccessDenial.MissingCapability,
            TemplateAreaAuthorization.Authorize(
                TemplateAreaOperation.PublishRevision,
                actor,
                AreaId
            )
        );
        Assert.Contains(
            TemplateAreaAccessDenial.MissingCapability,
            TemplateAreaAuthorization.Authorize(
                TemplateAreaOperation.ReadHrResponse,
                actor,
                AreaId
            )
        );
    }

    [Fact]
    public void Area_management_requires_capability_and_owner_group_assignment()
    {
        var actor = Scope(
            Set(FullProcedurePermissionKeys.CanManageTemplateAreas),
            Set(AreaId),
            Set(OtherOwnerGroupId)
        );

        var result = TemplateAreaAuthorization.Authorize(
            TemplateAreaOperation.ManageArea,
            actor,
            AreaId,
            OwnerGroupId
        );

        Assert.Equal([TemplateAreaAccessDenial.OwnerGroupNotAssigned], result);
    }

    private static TemplateAreaDraft ValidDraft() => new(
        "Environmental Monitoring",
        OwnerGroupId,
        ["environmental-monitoring"],
        ["location", "sample"],
        [],
        "micro-review"
    );

    private static TemplateAreaDefinition ValidArea()
    {
        var draft = ValidDraft();
        return new TemplateAreaDefinition(
            AreaId,
            draft.Name,
            draft.OwnerGroupId,
            draft.PurposeIds,
            draft.SubjectTypeIds,
            draft.CapabilityIds,
            draft.ReviewPolicyId
        );
    }

    private static TemplateAreaCatalog Catalog()
    {
        var environmental = new TemplatePurposeDescriptor(
            "environmental-monitoring",
            new HashSet<TemplateDefinitionKind>
            {
                TemplateDefinitionKind.Question,
                TemplateDefinitionKind.Form,
            },
            new HashSet<string> { "sample", "location" },
            new HashSet<string>()
        );
        var production = new TemplatePurposeDescriptor(
            "production-procedure",
            new HashSet<TemplateDefinitionKind>
            {
                TemplateDefinitionKind.Activity,
                TemplateDefinitionKind.Workflow,
            },
            new HashSet<string> { "product-batch" },
            new HashSet<string> { "controlled-stock-request" }
        );
        return new TemplateAreaCatalog(
            new Dictionary<string, TemplatePurposeDescriptor>
            {
                [environmental.Id] = environmental,
                [production.Id] = production,
            },
            new HashSet<string> { "sample", "location", "product-batch" },
            new HashSet<string> { "controlled-stock-request" },
            new HashSet<string> { "qa-review", "micro-review" },
            new HashSet<Guid> { OwnerGroupId }
        );
    }

    private static TemplateAreaActorScope Scope(
        IReadOnlySet<string> permissions,
        IReadOnlySet<Guid> areas,
        IReadOnlySet<Guid> ownerGroups) => new(permissions, areas, ownerGroups);

    private static IReadOnlySet<T> Set<T>(params T[] values) => values.ToHashSet();
}
