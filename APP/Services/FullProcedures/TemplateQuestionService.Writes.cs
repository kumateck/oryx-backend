using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FullProcedures;

public sealed partial class TemplateQuestionService
{
    public async Task<Result<TemplateQuestionRevisionDto>> CreateAsync(
        CreateTemplateQuestionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!TemplateQuestionServiceSupport.HasReason(request.Reason))
            return TemplateQuestionErrors.Invalid;
        var area = await LoadAreaAsync(request.TemplateAreaId, cancellationToken);
        if (area is null) return TemplateAreaErrors.NotFound;
        if (!area.IsActive) return TemplateQuestionErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(
                area, actorRoleIds, TemplateDefinitionOperation.Author))
            return TemplateQuestionErrors.AccessDenied;
        if (!ValidContext(area, request.PurposeId, request.SubjectTypeId))
            return TemplateQuestionErrors.Invalid;
        var questionId = Guid.NewGuid();
        var content = await ValidateContentAsync(
            request, area.Id, questionId, cancellationToken);
        if (content is null) return TemplateQuestionErrors.Invalid;

        var question = new TemplateQuestion
        {
            Id = questionId, TemplateAreaId = area.Id,
            PurposeId = request.PurposeId, SubjectTypeId = request.SubjectTypeId,
            CreatedById = actorId,
        };
        var revision = NewRevision(question.Id, 1, actorId, content);
        var audit = TemplateQuestionServiceSupport.Audit(
            revision, null, "DraftCreated", request.Reason, actorId, correlationId);
        revision.Audits.Add(audit);
        question.Revisions.Add(revision);
        context.Add(question);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateQuestionRevisionDto>> CreateRevisionAsync(
        Guid questionId, CreateTemplateQuestionRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!TemplateQuestionServiceSupport.HasReason(request.Reason))
            return TemplateQuestionErrors.Invalid;
        var question = await QuestionQuery().SingleOrDefaultAsync(
            item => item.Id == questionId, cancellationToken);
        if (question is null) return TemplateQuestionErrors.NotFound;
        if (!question.TemplateArea.IsActive) return TemplateQuestionErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(
                question.TemplateArea, actorRoleIds, TemplateDefinitionOperation.Author))
            return TemplateQuestionErrors.AccessDenied;
        if (question.Revisions.Any(item => item.Status is
                TemplateQuestionRevisionStatus.Draft or TemplateQuestionRevisionStatus.InReview))
            return TemplateQuestionErrors.Conflict;
        var content = await ValidateContentAsync(
            request, question.TemplateAreaId, question.Id, cancellationToken);
        if (content is null) return TemplateQuestionErrors.Invalid;
        var sequence = question.Revisions.Select(item => item.Sequence).DefaultIfEmpty().Max() + 1;
        var revision = NewRevision(question.Id, sequence, actorId, content);
        var audit = TemplateQuestionServiceSupport.Audit(
            revision, null, "DraftCreated", request.Reason, actorId, correlationId);
        revision.Audits.Add(audit);
        context.Add(revision);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateQuestionRevisionDto>> UpdateDraftAsync(
        Guid revisionId, UpdateTemplateQuestionRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!TemplateQuestionServiceSupport.HasReason(request.Reason))
            return TemplateQuestionErrors.Invalid;
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        if (revision is null) return TemplateQuestionErrors.NotFound;
        var area = revision.TemplateQuestion.TemplateArea;
        if (!area.IsActive) return TemplateQuestionErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(
                area, actorRoleIds, TemplateDefinitionOperation.Author))
            return TemplateQuestionErrors.AccessDenied;
        if (revision.Status != TemplateQuestionRevisionStatus.Draft ||
            revision.ContentHash != request.ExpectedContentHash)
            return TemplateQuestionErrors.Conflict;
        var content = await ValidateContentAsync(
            request, area.Id, revision.TemplateQuestionId, cancellationToken);
        if (content is null) return TemplateQuestionErrors.Invalid;

        context.Entry(revision).Property(item => item.ContentHash).OriginalValue =
            request.ExpectedContentHash;
        context.RemoveRange(revision.Options);
        context.RemoveRange(revision.CalculationReferences);
        TemplateQuestionServiceSupport.Apply(revision, content);
        context.AddRange(revision.Options.Cast<object>().Concat(revision.CalculationReferences));
        var audit = TemplateQuestionServiceSupport.Audit(
            revision, TemplateQuestionRevisionStatus.Draft, "DraftUpdated",
            request.Reason, actorId, correlationId);
        revision.Audits.Add(audit);
        context.Add(audit);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }
}
