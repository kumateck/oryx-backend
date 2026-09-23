using System.Data;
using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable

namespace APP.Services.FullProcedures;

public sealed partial class TemplateWorkflowService
{
    public Task<Result<TemplateWorkflowRevisionDto>> SubmitForReviewAsync(Guid revisionId,
        TemplateWorkflowTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default) => TransitionAsync(revisionId, request,
            actorId, actorRoleIds, correlationId, TemplateDefinitionOperation.Author,
            TemplateWorkflowRevisionStatus.Draft, TemplateWorkflowRevisionStatus.InReview,
            "SubmittedForReview", cancellationToken);

    public async Task<Result<TemplateWorkflowRevisionDto>> RecordReviewAsync(Guid revisionId,
        TemplateWorkflowTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = Guard(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Review, TemplateWorkflowRevisionStatus.InReview);
        if (guard is not null) return guard;
        if (revision!.CreatedById == actorId || revision.ReviewedById.HasValue)
            return TemplateWorkflowErrors.SegregationOfDuties;
        revision.ReviewedById = actorId;
        revision.ReviewedAt = DateTime.UtcNow;
        AddAudit(revision, revision.Status, "ReviewCompleted", request.Reason,
            actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateWorkflowRevisionDto>> ReturnToDraftAsync(Guid revisionId,
        TemplateWorkflowTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = Guard(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Review, TemplateWorkflowRevisionStatus.InReview);
        if (guard is not null) return guard;
        if (revision!.CreatedById == actorId) return TemplateWorkflowErrors.SegregationOfDuties;
        context.Entry(revision).Property(x => x.Status).OriginalValue =
            TemplateWorkflowRevisionStatus.InReview;
        revision.Status = TemplateWorkflowRevisionStatus.Draft;
        revision.ReviewedById = null;
        revision.ReviewedAt = null;
        AddAudit(revision, TemplateWorkflowRevisionStatus.InReview, "ReturnedToDraft",
            request.Reason, actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateWorkflowRevisionDto>> PublishAsync(Guid revisionId,
        TemplateWorkflowTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = Guard(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Publish, TemplateWorkflowRevisionStatus.InReview);
        if (guard is not null) return guard;
        if (!revision!.ReviewedById.HasValue || revision.CreatedById == actorId ||
            (revision.TemplateWorkflow.TemplateArea.ReviewPolicyId == "regulated-three-person" &&
             revision.ReviewedById == actorId))
            return TemplateWorkflowErrors.SegregationOfDuties;
        if (!await ReferencesAvailableAsync(revision, cancellationToken))
            return TemplateWorkflowErrors.DependencyUnavailable;
        var now = DateTime.UtcNow;
        var active = await RevisionQuery().SingleOrDefaultAsync(x =>
            x.TemplateWorkflowId == revision.TemplateWorkflowId &&
            x.Status == TemplateWorkflowRevisionStatus.Published, cancellationToken);
        if (active is not null)
        {
            active.Status = TemplateWorkflowRevisionStatus.Retired;
            active.RetiredAt = now;
            AddAudit(active, TemplateWorkflowRevisionStatus.Published, "Superseded",
                request.Reason, actorId, correlationId);
        }
        revision.Status = TemplateWorkflowRevisionStatus.Published;
        revision.PublishedById = actorId;
        revision.PublishedAt = now;
        AddAudit(revision, TemplateWorkflowRevisionStatus.InReview, "Published",
            request.Reason, actorId, correlationId);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException) { return TemplateWorkflowErrors.Conflict; }
        catch (DbUpdateException) { return TemplateWorkflowErrors.Conflict; }
        context.ChangeTracker.Clear();
        var saved = await RevisionQuery().AsNoTracking()
            .SingleAsync(x => x.Id == revisionId, cancellationToken);
        return TemplateWorkflowServiceSupport.ToDto(saved);
    }

    public async Task<Result<TemplateWorkflowRevisionDto>> RetireAsync(Guid revisionId,
        TemplateWorkflowTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = Guard(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Publish, TemplateWorkflowRevisionStatus.Published);
        if (guard is not null) return guard;
        if (revision!.CreatedById == actorId) return TemplateWorkflowErrors.SegregationOfDuties;
        context.Entry(revision).Property(x => x.Status).OriginalValue =
            TemplateWorkflowRevisionStatus.Published;
        revision.Status = TemplateWorkflowRevisionStatus.Retired;
        revision.RetiredAt = DateTime.UtcNow;
        AddAudit(revision, TemplateWorkflowRevisionStatus.Published, "Retired",
            request.Reason, actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    private async Task<Result<TemplateWorkflowRevisionDto>> TransitionAsync(Guid revisionId,
        TemplateWorkflowTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        TemplateDefinitionOperation operation, TemplateWorkflowRevisionStatus expected,
        TemplateWorkflowRevisionStatus next, string action, CancellationToken token)
    {
        var revision = await LoadRevisionAsync(revisionId, token);
        var guard = Guard(revision, request, actorRoleIds, operation, expected);
        if (guard is not null) return guard;
        context.Entry(revision!).Property(x => x.Status).OriginalValue = expected;
        revision!.Status = next;
        AddAudit(revision, expected, action, request.Reason, actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, token);
    }

    private void AddAudit(TemplateWorkflowRevision revision,
        TemplateWorkflowRevisionStatus? prior, string action, string reason,
        Guid actorId, Guid correlationId)
    {
        var audit = TemplateWorkflowServiceSupport.Audit(revision, prior, action,
            reason, actorId, correlationId);
        revision.Audits.Add(audit);
        context.Add(audit);
    }

    private static Error? Guard(TemplateWorkflowRevision? revision,
        TemplateWorkflowTransitionRequest request, IReadOnlyCollection<Guid> actorRoleIds,
        TemplateDefinitionOperation operation, TemplateWorkflowRevisionStatus expected)
    {
        if (revision is null) return TemplateWorkflowErrors.NotFound;
        if (!revision.TemplateWorkflow.TemplateArea.IsActive) return TemplateWorkflowErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(
                revision.TemplateWorkflow.TemplateArea, actorRoleIds, operation))
            return TemplateWorkflowErrors.AccessDenied;
        return revision.Status != expected || revision.ContentHash != request.ExpectedContentHash ||
               !TemplateWorkflowServiceSupport.HasReason(request.Reason)
            ? TemplateWorkflowErrors.Conflict : null;
    }
}
