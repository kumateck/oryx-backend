using DOMAIN.Entities.FullProcedures;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable

namespace APP.Services.FullProcedures;

public sealed partial class TemplateSectionService(ApplicationDbContext context)
    : ITemplateSectionService
{
    private IQueryable<TemplateArea> AreaQuery() => context.Set<TemplateArea>()
        .Include(item => item.RoleGrants).Include(item => item.Purposes)
        .Include(item => item.SubjectTypes);

    private IQueryable<TemplateSection> SectionQuery() => context.Set<TemplateSection>()
        .AsSplitQuery().Include(item => item.TemplateArea).ThenInclude(item => item.RoleGrants)
        .Include(item => item.Revisions).ThenInclude(item => item.Questions)
            .ThenInclude(item => item.TemplateQuestionRevision)
        .Include(item => item.Revisions).ThenInclude(item => item.ConditionalRules)
            .ThenInclude(item => item.TargetSectionQuestion)
        .Include(item => item.Revisions).ThenInclude(item => item.ConditionalRules)
            .ThenInclude(item => item.DependsOnSectionQuestion);

    private IQueryable<TemplateSectionRevision> RevisionQuery() =>
        context.Set<TemplateSectionRevision>().AsSplitQuery()
            .Include(item => item.TemplateSection).ThenInclude(item => item.TemplateArea)
                .ThenInclude(item => item.RoleGrants)
            .Include(item => item.Questions).ThenInclude(item => item.TemplateQuestionRevision)
            .Include(item => item.ConditionalRules).ThenInclude(item => item.TargetSectionQuestion)
            .Include(item => item.ConditionalRules).ThenInclude(item => item.DependsOnSectionQuestion);

    private Task<TemplateArea?> LoadAreaAsync(Guid id, CancellationToken cancellationToken) =>
        AreaQuery().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    private Task<TemplateSectionRevision?> LoadRevisionAsync(
        Guid id, CancellationToken cancellationToken) =>
        RevisionQuery().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    private async Task<TemplateSectionContent?> ResolveContentAsync(
        TemplateSectionContentRequest request, Guid areaId, string purposeId,
        string subjectTypeId, CancellationToken cancellationToken)
    {
        if (!TemplateSectionServiceSupport.ValidShape(request)) return null;
        var revisionIds = request.Questions.Select(item => item.RevisionId).ToArray();
        var resolved = await context.Set<TemplateQuestionRevision>().AsNoTracking()
            .Where(item => revisionIds.Contains(item.Id) &&
                item.Status == TemplateQuestionRevisionStatus.Published &&
                item.TemplateQuestion.TemplateAreaId == areaId &&
                item.TemplateQuestion.PurposeId == purposeId &&
                item.TemplateQuestion.SubjectTypeId == subjectTypeId)
            .Select(item => new
            {
                item.Id, item.TemplateQuestionId, item.Wording, item.AnswerType,
            }).ToListAsync(cancellationToken);
        if (resolved.Count != revisionIds.Length) return null;
        var byRevision = resolved.ToDictionary(item => item.Id,
            item => (item.TemplateQuestionId, item.Wording, item.AnswerType));
        if (request.Questions.Any(item =>
                byRevision[item.RevisionId].TemplateQuestionId != item.QuestionId))
            return null;
        return TemplateSectionServiceSupport.BuildContent(request, byRevision);
    }

    private static bool ValidContext(
        TemplateArea area, string purposeId, string subjectTypeId) =>
        area.Purposes.Any(item => item.PurposeId == purposeId) &&
        area.SubjectTypes.Any(item => item.SubjectTypeId == subjectTypeId);

    private static TemplateSectionRevision NewRevision(
        Guid sectionId, int sequence, Guid actorId, TemplateSectionContent content)
    {
        var revision = new TemplateSectionRevision
        {
            Id = Guid.NewGuid(), TemplateSectionId = sectionId, Sequence = sequence,
            Status = TemplateSectionRevisionStatus.Draft, CreatedById = actorId,
        };
        TemplateSectionServiceSupport.Apply(revision, content);
        return revision;
    }

    private async Task<bool> ReferencesArePublishedAsync(
        TemplateSectionRevision revision, CancellationToken cancellationToken)
    {
        var revisionIds = revision.Questions.Select(item => item.TemplateQuestionRevisionId)
            .ToArray();
        var pairs = await context.Set<TemplateQuestionRevision>().AsNoTracking()
            .Where(item => revisionIds.Contains(item.Id) &&
                item.Status == TemplateQuestionRevisionStatus.Published &&
                item.TemplateQuestion.TemplateAreaId == revision.TemplateSection.TemplateAreaId &&
                item.TemplateQuestion.PurposeId == revision.TemplateSection.PurposeId &&
                item.TemplateQuestion.SubjectTypeId == revision.TemplateSection.SubjectTypeId)
            .Select(item => new { item.Id, item.TemplateQuestionId })
            .ToListAsync(cancellationToken);
        var resolved = pairs.ToDictionary(item => item.Id, item => item.TemplateQuestionId);
        return revision.Questions.All(item => resolved.TryGetValue(
            item.TemplateQuestionRevisionId, out var questionId) &&
            questionId == item.TemplateQuestionId);
    }

    private async Task<Result<TemplateSectionRevisionDto>> SaveRevisionAsync(
        Guid revisionId, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return TemplateSectionErrors.Conflict;
        }
        catch (DbUpdateException)
        {
            return TemplateSectionErrors.Conflict;
        }
        context.ChangeTracker.Clear();
        var saved = await RevisionQuery().AsNoTracking()
            .SingleAsync(item => item.Id == revisionId, cancellationToken);
        return TemplateSectionServiceSupport.ToDto(saved);
    }

    private static TemplateSectionDto ToSectionDto(TemplateSection section)
    {
        var latest = section.Revisions.OrderByDescending(item => item.Sequence).FirstOrDefault();
        return new TemplateSectionDto(
            section.Id, section.TemplateAreaId, section.TemplateArea.Name,
            section.PurposeId, section.SubjectTypeId,
            latest is null ? null : TemplateSectionServiceSupport.ToDto(latest));
    }
}
