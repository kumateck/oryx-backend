using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.FullProcedures;

public sealed partial class TemplateAdoptionService
{
    private async Task<AdoptionDraftRequest> BuildFormAsync(TemplateSharingGrant grant,
        AdoptTemplateRevisionRequest request, CancellationToken token)
    {
        var source = await context.Set<TemplateFormRevision>().AsNoTracking().AsSplitQuery()
            .Include(x => x.Sections).Include(x => x.ConditionalRules)
                .ThenInclude(x => x.TargetFormSection)
            .Include(x => x.ConditionalRules).ThenInclude(x => x.SourceFormSection)
            .SingleOrDefaultAsync(x => x.Id == grant.RevisionId &&
                x.TemplateFormId == grant.DefinitionId &&
                x.TemplateForm.TemplateAreaId == grant.SourceAreaId &&
                x.Status == TemplateFormRevisionStatus.Published &&
                x.ContentHash == grant.RevisionContentHash, token);
        if (source is null || request.RoleMappings.Count != 0) return null;
        var expected = source.Sections.Select(x =>
                (x.TemplateSectionId, x.TemplateSectionRevisionId))
            .Concat(source.ConditionalRules.Select(x =>
                (x.SourceQuestionId, x.SourceQuestionRevisionId))).Distinct().ToArray();
        if (!ExactDependencySet(request, expected) ||
            !await TargetFormDependenciesAvailableAsync(grant, source, request, token))
            return null;
        var sectionMap = source.Sections.ToDictionary(x => x.TemplateSectionId, x =>
            FindDependency(request, x.TemplateSectionId, x.TemplateSectionRevisionId));
        return new AdoptionDraftRequest(TemplateRevisionKind.Form,
            new CreateTemplateFormRequest
            {
                TemplateAreaId = grant.TargetAreaId, PurposeId = grant.PurposeId,
                SubjectTypeId = grant.SubjectTypeId, Reason = request.Reason,
                Name = source.Name, Description = source.Description,
                RequiresEvidence = source.RequiresEvidence,
                RequiresSignature = source.RequiresSignature,
                Sections = source.Sections.OrderBy(x => x.Order).Select(x =>
                {
                    var map = sectionMap[x.TemplateSectionId];
                    return new TemplateFormSectionRequest
                    {
                        SectionId = map.TargetDefinitionId,
                        RevisionId = map.TargetRevisionId, Order = x.Order,
                        IsRequired = x.IsRequired,
                    };
                }).ToList(),
                ConditionalRules = source.ConditionalRules.Select(x =>
                {
                    var question = FindDependency(
                        request, x.SourceQuestionId, x.SourceQuestionRevisionId);
                    return new TemplateFormConditionalRuleRequest
                    {
                        TargetSectionId = sectionMap[x.TargetFormSection.TemplateSectionId]
                            .TargetDefinitionId,
                        SourceSectionId = sectionMap[x.SourceFormSection.TemplateSectionId]
                            .TargetDefinitionId,
                        SourceQuestionId = question.TargetDefinitionId,
                        SourceQuestionRevisionId = question.TargetRevisionId,
                        Operator = x.Operator, Value = x.ComparisonValue,
                    };
                }).ToList(),
            });
    }

    private async Task<AdoptionDraftRequest> BuildActivityAsync(TemplateSharingGrant grant,
        AdoptTemplateRevisionRequest request, CancellationToken token)
    {
        var source = await context.Set<TemplateActivityRevision>().AsNoTracking().AsSplitQuery()
            .Include(x => x.Forms).Include(x => x.Actions).ThenInclude(x => x.Roles)
            .Include(x => x.Resources).Include(x => x.DataBindings)
            .Include(x => x.CompletionRules)
            .SingleOrDefaultAsync(x => x.Id == grant.RevisionId &&
                x.TemplateActivityId == grant.DefinitionId &&
                x.TemplateActivity.TemplateAreaId == grant.SourceAreaId &&
                x.Status == TemplateActivityRevisionStatus.Published &&
                x.ContentHash == grant.RevisionContentHash, token);
        if (source is null) return null;
        var expectedDependencies = source.Forms.Select(x =>
            (x.TemplateFormId, x.TemplateFormRevisionId)).ToArray();
        var expectedRoles = source.Actions.SelectMany(x => x.Roles)
            .Select(x => x.RoleId).Distinct().ToArray();
        if (!ExactDependencySet(request, expectedDependencies) ||
            !ExactRoleSet(request, expectedRoles) ||
            !await TargetFormsAvailableAsync(grant, request.DependencyMappings, token))
            return null;
        var formMap = source.Forms.ToDictionary(x => x.TemplateFormId, x =>
            FindDependency(request, x.TemplateFormId, x.TemplateFormRevisionId));
        return new AdoptionDraftRequest(TemplateRevisionKind.Activity,
            new CreateTemplateActivityRequest
            {
                TemplateAreaId = grant.TargetAreaId, PurposeId = grant.PurposeId,
                SubjectTypeId = grant.SubjectTypeId, Reason = request.Reason,
                Name = source.Name, Instructions = source.Instructions,
                Forms = source.Forms.OrderBy(x => x.Order).Select(x =>
                {
                    var map = formMap[x.TemplateFormId];
                    return new TemplateActivityFormBindingRequest
                    {
                        FormId = map.TargetDefinitionId, RevisionId = map.TargetRevisionId,
                        Key = x.Key, Order = x.Order, Usage = x.Usage,
                        IsRequired = x.IsRequired,
                    };
                }).ToList(),
                Actions = source.Actions.OrderBy(x => x.Order).Select(x =>
                    new TemplateActivityActionRequest
                    {
                        Key = x.Key, Name = x.Name, Order = x.Order,
                        ActionType = x.ActionType,
                        RequiresIndependentChecker = x.RequiresIndependentChecker,
                        RequiresApproval = x.RequiresApproval,
                        PerformerRoleIds = MapRoles(request, x, TemplateActivityActionRoleKind.Performer),
                        CheckerRoleIds = MapRoles(request, x, TemplateActivityActionRoleKind.Checker),
                        ApproverRoleIds = MapRoles(request, x, TemplateActivityActionRoleKind.Approver),
                    }).ToList(),
                Resources = source.Resources.OrderBy(x => x.Order).Select(x =>
                    new TemplateActivityResourceRequest
                    { CapabilityId = x.CapabilityId, Order = x.Order, IsRequired = x.IsRequired })
                    .ToList(),
                DataBindings = source.DataBindings.OrderBy(x => x.Order).Select(x =>
                    new TemplateActivityDataBindingRequest
                    { Key = x.Key, Order = x.Order, Direction = x.Direction,
                        DataType = x.DataType, IsRequired = x.IsRequired }).ToList(),
                CompletionRules = source.CompletionRules.OrderBy(x => x.Order).Select(x =>
                    new TemplateActivityCompletionRuleRequest
                    { Order = x.Order, RuleType = x.RuleType, TargetKey = x.TargetKey }).ToList(),
            });
    }

    private static List<Guid> MapRoles(AdoptTemplateRevisionRequest request,
        TemplateActivityAction action, TemplateActivityActionRoleKind kind) => action.Roles
        .Where(x => x.RoleKind == kind).Select(x => MapRole(request, x.RoleId)).ToList();

    private async Task<bool> TargetFormsAvailableAsync(TemplateSharingGrant grant,
        IReadOnlyCollection<TemplateAdoptionDependencyMappingRequest> mappings,
        CancellationToken token)
    {
        if (mappings.Count == 0) return true;
        var revisionIds = mappings.Select(x => x.TargetRevisionId).ToArray();
        var pairs = await context.Set<TemplateFormRevision>().AsNoTracking()
            .Where(x => revisionIds.Contains(x.Id) && x.Status == TemplateFormRevisionStatus.Published &&
                x.TemplateForm.TemplateAreaId == grant.TargetAreaId &&
                x.TemplateForm.PurposeId == grant.PurposeId &&
                x.TemplateForm.SubjectTypeId == grant.SubjectTypeId)
            .Select(x => new { x.TemplateFormId, x.Id }).ToListAsync(token);
        var set = pairs.Select(x => (x.TemplateFormId, x.Id)).ToHashSet();
        return set.Count == mappings.Count && mappings.All(x =>
            set.Contains((x.TargetDefinitionId, x.TargetRevisionId)));
    }
}
