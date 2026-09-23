using System.Data;
using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable

namespace APP.Services.FullProcedures;

public sealed partial class TemplateQuestionService
{
    public Task<Result<TemplateQuestionRevisionDto>> SubmitForReviewAsync(
        Guid revisionId, TemplateQuestionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default) => TransitionAsync(
            revisionId, request, actorId, actorRoleIds, correlationId,
            TemplateDefinitionOperation.Author, TemplateQuestionRevisionStatus.Draft,
            TemplateQuestionRevisionStatus.InReview, "SubmittedForReview", cancellationToken);

    public async Task<Result<TemplateQuestionRevisionDto>> RecordReviewAsync(
        Guid revisionId, TemplateQuestionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = GuardTransition(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Review, TemplateQuestionRevisionStatus.InReview);
        if (guard is not null) return guard;
        if (revision!.CreatedById == actorId || revision.ReviewedById.HasValue)
            return TemplateQuestionErrors.SegregationOfDuties;
        revision.ReviewedById = actorId;
        revision.ReviewedAt = DateTime.UtcNow;
        var audit = TemplateQuestionServiceSupport.Audit(
            revision, revision.Status, "ReviewCompleted", request.Reason,
            actorId, correlationId);
        revision.Audits.Add(audit);
        context.Add(audit);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateQuestionRevisionDto>> ReturnToDraftAsync(
        Guid revisionId, TemplateQuestionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = GuardTransition(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Review, TemplateQuestionRevisionStatus.InReview);
        if (guard is not null) return guard;
        if (revision!.CreatedById == actorId)
            return TemplateQuestionErrors.SegregationOfDuties;
        context.Entry(revision).Property(item => item.Status).OriginalValue =
            TemplateQuestionRevisionStatus.InReview;
        revision.Status = TemplateQuestionRevisionStatus.Draft;
        revision.ReviewedById = null;
        revision.ReviewedAt = null;
        var audit = TemplateQuestionServiceSupport.Audit(
            revision, TemplateQuestionRevisionStatus.InReview, "ReturnedToDraft",
            request.Reason, actorId, correlationId);
        revision.Audits.Add(audit);
        context.Add(audit);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateQuestionRevisionDto>> PublishAsync(
        Guid revisionId, TemplateQuestionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken) : null;
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = GuardTransition(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Publish, TemplateQuestionRevisionStatus.InReview);
        if (guard is not null) return guard;
        if (!revision!.ReviewedById.HasValue || revision.CreatedById == actorId ||
            (revision.TemplateQuestion.TemplateArea.ReviewPolicyId == "regulated-three-person" &&
             revision.ReviewedById == actorId))
            return TemplateQuestionErrors.SegregationOfDuties;

        var now = DateTime.UtcNow;
        var active = await RevisionQuery().SingleOrDefaultAsync(item =>
            item.TemplateQuestionId == revision.TemplateQuestionId &&
            item.Status == TemplateQuestionRevisionStatus.Published, cancellationToken);
        if (active is not null)
        {
            active.Status = TemplateQuestionRevisionStatus.Retired;
            active.RetiredAt = now;
            var superseded = TemplateQuestionServiceSupport.Audit(
                active, TemplateQuestionRevisionStatus.Published, "Superseded",
                request.Reason, actorId, correlationId);
            active.Audits.Add(superseded);
            context.Add(superseded);
        }
        revision.Status = TemplateQuestionRevisionStatus.Published;
        revision.PublishedById = actorId;
        revision.PublishedAt = now;
        var published = TemplateQuestionServiceSupport.Audit(
            revision, TemplateQuestionRevisionStatus.InReview, "Published",
            request.Reason, actorId, correlationId);
        revision.Audits.Add(published);
        context.Add(published);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
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

    public async Task<Result<TemplateQuestionRevisionDto>> RetireAsync(
        Guid revisionId, TemplateQuestionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = GuardTransition(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Publish, TemplateQuestionRevisionStatus.Published);
        if (guard is not null) return guard;
        if (revision!.CreatedById == actorId)
            return TemplateQuestionErrors.SegregationOfDuties;
        context.Entry(revision).Property(item => item.Status).OriginalValue =
            TemplateQuestionRevisionStatus.Published;
        revision.Status = TemplateQuestionRevisionStatus.Retired;
        revision.RetiredAt = DateTime.UtcNow;
        var audit = TemplateQuestionServiceSupport.Audit(
            revision, TemplateQuestionRevisionStatus.Published, "Retired",
            request.Reason, actorId, correlationId);
        revision.Audits.Add(audit);
        context.Add(audit);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    private async Task<Result<TemplateQuestionRevisionDto>> TransitionAsync(
        Guid revisionId, TemplateQuestionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        TemplateDefinitionOperation operation, TemplateQuestionRevisionStatus expected,
        TemplateQuestionRevisionStatus next, string action, CancellationToken cancellationToken)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = GuardTransition(revision, request, actorRoleIds, operation, expected);
        if (guard is not null) return guard;
        context.Entry(revision!).Property(item => item.Status).OriginalValue = expected;
        revision!.Status = next;
        var audit = TemplateQuestionServiceSupport.Audit(
            revision, expected, action, request.Reason, actorId, correlationId);
        revision.Audits.Add(audit);
        context.Add(audit);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    private static Error? GuardTransition(
        TemplateQuestionRevision? revision, TemplateQuestionTransitionRequest request,
        IReadOnlyCollection<Guid> actorRoleIds, TemplateDefinitionOperation operation,
        TemplateQuestionRevisionStatus expected)
    {
        if (revision is null) return TemplateQuestionErrors.NotFound;
        if (!revision.TemplateQuestion.TemplateArea.IsActive)
            return TemplateQuestionErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(
                revision.TemplateQuestion.TemplateArea, actorRoleIds, operation))
            return TemplateQuestionErrors.AccessDenied;
        return revision.Status != expected ||
               revision.ContentHash != request.ExpectedContentHash ||
               !TemplateQuestionServiceSupport.HasReason(request.Reason)
            ? TemplateQuestionErrors.Conflict : null;
    }
}
