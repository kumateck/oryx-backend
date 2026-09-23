using System.Data;
using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable

namespace APP.Services.FullProcedures;

public sealed partial class TemplateActivityService
{
    public Task<Result<TemplateActivityRevisionDto>> SubmitForReviewAsync(Guid revisionId,
        TemplateActivityTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default) => TransitionAsync(revisionId, request,
            actorId, actorRoleIds, correlationId, TemplateDefinitionOperation.Author,
            TemplateActivityRevisionStatus.Draft, TemplateActivityRevisionStatus.InReview,
            "SubmittedForReview", cancellationToken);

    public async Task<Result<TemplateActivityRevisionDto>> RecordReviewAsync(Guid revisionId,
        TemplateActivityTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = Guard(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Review, TemplateActivityRevisionStatus.InReview);
        if (guard is not null) return guard;
        if (revision!.CreatedById == actorId || revision.ReviewedById.HasValue)
            return TemplateActivityErrors.SegregationOfDuties;
        revision.ReviewedById = actorId;
        revision.ReviewedAt = DateTime.UtcNow;
        AddAudit(revision, revision.Status, "ReviewCompleted", request.Reason,
            actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateActivityRevisionDto>> ReturnToDraftAsync(Guid revisionId,
        TemplateActivityTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = Guard(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Review, TemplateActivityRevisionStatus.InReview);
        if (guard is not null) return guard;
        if (revision!.CreatedById == actorId) return TemplateActivityErrors.SegregationOfDuties;
        context.Entry(revision).Property(x => x.Status).OriginalValue =
            TemplateActivityRevisionStatus.InReview;
        revision.Status = TemplateActivityRevisionStatus.Draft;
        revision.ReviewedById = null;
        revision.ReviewedAt = null;
        AddAudit(revision, TemplateActivityRevisionStatus.InReview, "ReturnedToDraft",
            request.Reason, actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateActivityRevisionDto>> PublishAsync(Guid revisionId,
        TemplateActivityTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = Guard(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Publish, TemplateActivityRevisionStatus.InReview);
        if (guard is not null) return guard;
        if (!revision!.ReviewedById.HasValue || revision.CreatedById == actorId ||
            (revision.TemplateActivity.TemplateArea.ReviewPolicyId == "regulated-three-person" &&
             revision.ReviewedById == actorId))
            return TemplateActivityErrors.SegregationOfDuties;
        if (!await ReferencesAvailableAsync(revision, cancellationToken))
            return TemplateActivityErrors.DependencyUnavailable;
        var now = DateTime.UtcNow;
        var active = await RevisionQuery().SingleOrDefaultAsync(x =>
            x.TemplateActivityId == revision.TemplateActivityId &&
            x.Status == TemplateActivityRevisionStatus.Published, cancellationToken);
        if (active is not null)
        {
            active.Status = TemplateActivityRevisionStatus.Retired;
            active.RetiredAt = now;
            AddAudit(active, TemplateActivityRevisionStatus.Published, "Superseded",
                request.Reason, actorId, correlationId);
        }
        revision.Status = TemplateActivityRevisionStatus.Published;
        revision.PublishedById = actorId;
        revision.PublishedAt = now;
        AddAudit(revision, TemplateActivityRevisionStatus.InReview, "Published",
            request.Reason, actorId, correlationId);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException) { return TemplateActivityErrors.Conflict; }
        catch (DbUpdateException) { return TemplateActivityErrors.Conflict; }
        context.ChangeTracker.Clear();
        var saved = await RevisionQuery().AsNoTracking()
            .SingleAsync(x => x.Id == revisionId, cancellationToken);
        return TemplateActivityServiceSupport.ToDto(saved);
    }

    public async Task<Result<TemplateActivityRevisionDto>> RetireAsync(Guid revisionId,
        TemplateActivityTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = Guard(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Publish, TemplateActivityRevisionStatus.Published);
        if (guard is not null) return guard;
        if (revision!.CreatedById == actorId) return TemplateActivityErrors.SegregationOfDuties;
        context.Entry(revision).Property(x => x.Status).OriginalValue =
            TemplateActivityRevisionStatus.Published;
        revision.Status = TemplateActivityRevisionStatus.Retired;
        revision.RetiredAt = DateTime.UtcNow;
        AddAudit(revision, TemplateActivityRevisionStatus.Published, "Retired",
            request.Reason, actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    private async Task<Result<TemplateActivityRevisionDto>> TransitionAsync(Guid revisionId,
        TemplateActivityTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        TemplateDefinitionOperation operation, TemplateActivityRevisionStatus expected,
        TemplateActivityRevisionStatus next, string action, CancellationToken token)
    {
        var revision = await LoadRevisionAsync(revisionId, token);
        var guard = Guard(revision, request, actorRoleIds, operation, expected);
        if (guard is not null) return guard;
        context.Entry(revision!).Property(x => x.Status).OriginalValue = expected;
        revision!.Status = next;
        AddAudit(revision, expected, action, request.Reason, actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, token);
    }

    private void AddAudit(TemplateActivityRevision revision,
        TemplateActivityRevisionStatus? prior, string action, string reason,
        Guid actorId, Guid correlationId)
    {
        var audit = TemplateActivityServiceSupport.Audit(revision, prior, action,
            reason, actorId, correlationId);
        revision.Audits.Add(audit);
        context.Add(audit);
    }

    private static Error? Guard(TemplateActivityRevision? revision,
        TemplateActivityTransitionRequest request, IReadOnlyCollection<Guid> actorRoleIds,
        TemplateDefinitionOperation operation, TemplateActivityRevisionStatus expected)
    {
        if (revision is null) return TemplateActivityErrors.NotFound;
        if (!revision.TemplateActivity.TemplateArea.IsActive) return TemplateActivityErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(
                revision.TemplateActivity.TemplateArea, actorRoleIds, operation))
            return TemplateActivityErrors.AccessDenied;
        return revision.Status != expected || revision.ContentHash != request.ExpectedContentHash ||
               !TemplateActivityServiceSupport.HasReason(request.Reason)
            ? TemplateActivityErrors.Conflict : null;
    }
}
