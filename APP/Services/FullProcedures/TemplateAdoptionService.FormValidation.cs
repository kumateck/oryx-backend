using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.FullProcedures;

public sealed partial class TemplateAdoptionService
{
    private async Task<bool> TargetFormDependenciesAvailableAsync(TemplateSharingGrant grant,
        TemplateFormRevision source, AdoptTemplateRevisionRequest request,
        CancellationToken token)
    {
        var sectionSourcePairs = source.Sections
            .Select(x => (x.TemplateSectionId, x.TemplateSectionRevisionId)).ToHashSet();
        var sectionMappings = request.DependencyMappings.Where(x =>
            sectionSourcePairs.Contains((x.SourceDefinitionId, x.SourceRevisionId))).ToArray();
        var sectionRevisionIds = sectionMappings.Select(x => x.TargetRevisionId).ToArray();
        var targetSections = await context.Set<TemplateSectionRevision>().AsNoTracking()
            .Include(x => x.Questions)
            .Where(x => sectionRevisionIds.Contains(x.Id) &&
                x.Status == TemplateSectionRevisionStatus.Published &&
                x.TemplateSection.TemplateAreaId == grant.TargetAreaId &&
                x.TemplateSection.PurposeId == grant.PurposeId &&
                x.TemplateSection.SubjectTypeId == grant.SubjectTypeId)
            .ToListAsync(token);
        var sectionPairs = targetSections.Select(x => (x.TemplateSectionId, x.Id)).ToHashSet();
        if (sectionPairs.Count != sectionMappings.Length || sectionMappings.Any(x =>
                !sectionPairs.Contains((x.TargetDefinitionId, x.TargetRevisionId)))) return false;

        var questionSourcePairs = source.ConditionalRules
            .Select(x => (x.SourceQuestionId, x.SourceQuestionRevisionId)).Distinct().ToHashSet();
        var questionMappings = request.DependencyMappings.Where(x =>
            questionSourcePairs.Contains((x.SourceDefinitionId, x.SourceRevisionId))).ToArray();
        if (!await TargetQuestionsAvailableAsync(grant, questionMappings, token)) return false;
        foreach (var rule in source.ConditionalRules)
        {
            var sourceSection = source.Sections.Single(x =>
                x.Id == rule.SourceFormSectionId);
            var sectionMap = FindDependency(request, sourceSection.TemplateSectionId,
                sourceSection.TemplateSectionRevisionId);
            var questionMap = FindDependency(request, rule.SourceQuestionId,
                rule.SourceQuestionRevisionId);
            var targetSection = targetSections.SingleOrDefault(x =>
                x.Id == sectionMap.TargetRevisionId &&
                x.TemplateSectionId == sectionMap.TargetDefinitionId);
            if (targetSection is null || !targetSection.Questions.Any(x =>
                    x.TemplateQuestionId == questionMap.TargetDefinitionId &&
                    x.TemplateQuestionRevisionId == questionMap.TargetRevisionId)) return false;
        }
        return true;
    }
}
