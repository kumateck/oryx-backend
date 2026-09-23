using DOMAIN.Entities.FullProcedures;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable

namespace APP.Services.FullProcedures;

public sealed partial class TemplateQuestionService(ApplicationDbContext context)
    : ITemplateQuestionService
{
    private IQueryable<TemplateArea> AreaQuery() => context.Set<TemplateArea>()
        .Include(item => item.RoleGrants).Include(item => item.Purposes)
        .Include(item => item.SubjectTypes);

    private IQueryable<TemplateQuestion> QuestionQuery() => context.Set<TemplateQuestion>()
        .AsSplitQuery().Include(item => item.TemplateArea).ThenInclude(item => item.RoleGrants)
        .Include(item => item.Revisions).ThenInclude(item => item.Options)
        .Include(item => item.Revisions).ThenInclude(item => item.CalculationReferences)
        .Include(item => item.Revisions).ThenInclude(item => item.UnitOfMeasure);

    private IQueryable<TemplateQuestionRevision> RevisionQuery() =>
        context.Set<TemplateQuestionRevision>().AsSplitQuery()
            .Include(item => item.TemplateQuestion).ThenInclude(item => item.TemplateArea)
                .ThenInclude(item => item.RoleGrants)
            .Include(item => item.Options).Include(item => item.CalculationReferences)
            .Include(item => item.UnitOfMeasure);

    private Task<TemplateArea?> LoadAreaAsync(Guid id, CancellationToken cancellationToken) =>
        AreaQuery().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    private Task<TemplateQuestionRevision?> LoadRevisionAsync(
        Guid id, CancellationToken cancellationToken) =>
        RevisionQuery().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    private async Task<TemplateQuestionContent?> ValidateContentAsync(
        TemplateQuestionContentRequest request, Guid areaId, Guid questionId,
        CancellationToken cancellationToken)
    {
        var content = TemplateQuestionServiceSupport.Prepare(request);
        if (content is null || content.CalculationReferences.Any(item => item.QuestionId == questionId))
            return null;
        if (content.UnitOfMeasureId.HasValue && !await context.UnitOfMeasures.AsNoTracking()
                .AnyAsync(item => item.Id == content.UnitOfMeasureId.Value, cancellationToken))
            return null;
        if (content.CalculationReferences.Count == 0) return content;
        var questionIds = content.CalculationReferences.Select(item => item.QuestionId).ToArray();
        var references = await context.Set<TemplateQuestionRevision>().AsNoTracking()
            .Where(item => questionIds.Contains(item.TemplateQuestionId) &&
                item.TemplateQuestion.TemplateAreaId == areaId &&
                item.Status == TemplateQuestionRevisionStatus.Published)
            .Select(item => new TemplateQuestionReference(item.TemplateQuestionId, item.Id))
            .ToListAsync(cancellationToken);
        return references.Count == questionIds.Length ? content with
            { CalculationReferences = references.OrderBy(item => item.QuestionId).ToArray() } : null;
    }

    private static bool ValidContext(
        TemplateArea area, string purposeId, string subjectTypeId) =>
        area.Purposes.Any(item => item.PurposeId == purposeId) &&
        area.SubjectTypes.Any(item => item.SubjectTypeId == subjectTypeId);

    private static TemplateQuestionRevision NewRevision(
        Guid questionId, int sequence, Guid actorId, TemplateQuestionContent content)
    {
        var revision = new TemplateQuestionRevision
        {
            Id = Guid.NewGuid(), TemplateQuestionId = questionId, Sequence = sequence,
            Status = TemplateQuestionRevisionStatus.Draft, CreatedById = actorId,
        };
        TemplateQuestionServiceSupport.Apply(revision, content);
        return revision;
    }

    private async Task<Result<TemplateQuestionRevisionDto>> SaveRevisionAsync(
        Guid revisionId, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return TemplateQuestionErrors.Conflict;
        }
        catch (DbUpdateException)
        {
            return TemplateQuestionErrors.Conflict;
        }
        context.ChangeTracker.Clear();
        var saved = await RevisionQuery().AsNoTracking()
            .SingleAsync(item => item.Id == revisionId, cancellationToken);
        return TemplateQuestionServiceSupport.ToRevisionDto(saved);
    }

    private static TemplateQuestionDto ToQuestionDto(TemplateQuestion question)
    {
        var latest = question.Revisions.OrderByDescending(item => item.Sequence).FirstOrDefault();
        return new TemplateQuestionDto(
            question.Id, question.TemplateAreaId, question.TemplateArea.Name,
            question.PurposeId, question.SubjectTypeId,
            latest is null ? null : TemplateQuestionServiceSupport.ToRevisionDto(latest));
    }
}
