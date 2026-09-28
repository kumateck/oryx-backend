using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.FullProcedures;

public sealed partial class TemplateAdoptionService
{
    private async Task<AdoptionDraftRequest> BuildQuestionAsync(TemplateSharingGrant grant,
        AdoptTemplateRevisionRequest request, CancellationToken token)
    {
        var source = await context.Set<TemplateQuestionRevision>().AsNoTracking().AsSplitQuery()
            .Include(x => x.Options).Include(x => x.CalculationReferences)
            .SingleOrDefaultAsync(x => x.Id == grant.RevisionId &&
                x.TemplateQuestionId == grant.DefinitionId &&
                x.TemplateQuestion.TemplateAreaId == grant.SourceAreaId &&
                x.Status == TemplateQuestionRevisionStatus.Published &&
                x.ContentHash == grant.RevisionContentHash, token);
        if (source is null || request.RoleMappings.Count != 0) return null;
        var expected = source.CalculationReferences
            .Select(x => (x.ReferencedQuestionId, x.ReferencedQuestionRevisionId)).ToArray();
        if (!ExactDependencySet(request, expected) ||
            !await TargetQuestionsAvailableAsync(grant, request.DependencyMappings, token))
            return null;
        var targetReferences = expected.Select(x => FindDependency(request, x.Item1, x.Item2))
            .Select(x => new TemplateQuestionReferenceRequest
            {
                QuestionId = x.TargetDefinitionId, RevisionId = x.TargetRevisionId,
            }).ToList();
        return new AdoptionDraftRequest(TemplateRevisionKind.Question,
            new CreateTemplateQuestionRequest
            {
                TemplateAreaId = grant.TargetAreaId, PurposeId = grant.PurposeId,
                SubjectTypeId = grant.SubjectTypeId, Reason = request.Reason,
                Wording = source.Wording, AnswerType = source.AnswerType,
                InputType = source.InputType, UnitOfMeasureId = source.UnitOfMeasureId,
                Options = source.Options.OrderBy(x => x.Rank).Select(x =>
                    new TemplateQuestionOptionRequest { Value = x.Value, Label = x.Label }).ToList(),
                Required = source.Required, HelpText = source.HelpText,
                Minimum = source.Minimum, Maximum = source.Maximum,
                CalculationReferences = targetReferences, Sensitivity = source.Sensitivity,
            });
    }

    private async Task<AdoptionDraftRequest> BuildSectionAsync(TemplateSharingGrant grant,
        AdoptTemplateRevisionRequest request, CancellationToken token)
    {
        var source = await context.Set<TemplateSectionRevision>().AsNoTracking().AsSplitQuery()
            .Include(x => x.Questions).Include(x => x.ConditionalRules)
                .ThenInclude(x => x.TargetSectionQuestion)
            .Include(x => x.ConditionalRules).ThenInclude(x => x.DependsOnSectionQuestion)
            .SingleOrDefaultAsync(x => x.Id == grant.RevisionId &&
                x.TemplateSectionId == grant.DefinitionId &&
                x.TemplateSection.TemplateAreaId == grant.SourceAreaId &&
                x.Status == TemplateSectionRevisionStatus.Published &&
                x.ContentHash == grant.RevisionContentHash, token);
        if (source is null || request.RoleMappings.Count != 0) return null;
        var expected = source.Questions
            .Select(x => (x.TemplateQuestionId, x.TemplateQuestionRevisionId)).ToArray();
        if (!ExactDependencySet(request, expected) ||
            !await TargetQuestionsAvailableAsync(grant, request.DependencyMappings, token))
            return null;
        var questionMap = source.Questions.ToDictionary(x => x.TemplateQuestionId, x =>
            FindDependency(request, x.TemplateQuestionId, x.TemplateQuestionRevisionId));
        return new AdoptionDraftRequest(TemplateRevisionKind.Section,
            new CreateTemplateSectionRequest
            {
                TemplateAreaId = grant.TargetAreaId, PurposeId = grant.PurposeId,
                SubjectTypeId = grant.SubjectTypeId, Reason = request.Reason,
                Title = source.Title,
                Questions = source.Questions.OrderBy(x => x.Order).Select(x =>
                {
                    var map = questionMap[x.TemplateQuestionId];
                    return new TemplateSectionQuestionRequest
                    {
                        QuestionId = map.TargetDefinitionId,
                        RevisionId = map.TargetRevisionId, Order = x.Order,
                    };
                }).ToList(),
                ConditionalRules = source.ConditionalRules.Select(x =>
                    new TemplateSectionConditionalRuleRequest
                    {
                        TargetQuestionId = questionMap[x.TargetSectionQuestion.TemplateQuestionId]
                            .TargetDefinitionId,
                        DependsOnQuestionId = questionMap[x.DependsOnSectionQuestion.TemplateQuestionId]
                            .TargetDefinitionId,
                        Operator = x.Operator, Value = x.ComparisonValue,
                    }).ToList(),
            });
    }

    private async Task<bool> TargetQuestionsAvailableAsync(TemplateSharingGrant grant,
        IReadOnlyCollection<TemplateAdoptionDependencyMappingRequest> mappings,
        CancellationToken token)
    {
        if (mappings.Count == 0) return true;
        var revisionIds = mappings.Select(x => x.TargetRevisionId).ToArray();
        var targets = await context.Set<TemplateQuestionRevision>().AsNoTracking()
            .Where(x => revisionIds.Contains(x.Id) &&
                x.Status == TemplateQuestionRevisionStatus.Published &&
                x.TemplateQuestion.TemplateAreaId == grant.TargetAreaId &&
                x.TemplateQuestion.PurposeId == grant.PurposeId &&
                x.TemplateQuestion.SubjectTypeId == grant.SubjectTypeId)
            .Select(x => new { x.Id, x.TemplateQuestionId }).ToListAsync(token);
        var pairs = targets.Select(x => (x.TemplateQuestionId, x.Id)).ToHashSet();
        return pairs.Count == mappings.Count && mappings.All(x =>
            pairs.Contains((x.TargetDefinitionId, x.TargetRevisionId)));
    }
}
