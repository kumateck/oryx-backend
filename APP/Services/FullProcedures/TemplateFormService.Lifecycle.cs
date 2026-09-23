using System.Data;
using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable

namespace APP.Services.FullProcedures;

public sealed partial class TemplateFormService
{
    public Task<Result<TemplateFormRevisionDto>> SubmitForReviewAsync(
        Guid revisionId, TemplateFormTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default) => TransitionAsync(
            revisionId, request, actorId, actorRoleIds, correlationId,
            TemplateDefinitionOperation.Author, TemplateFormRevisionStatus.Draft,
            TemplateFormRevisionStatus.InReview, "SubmittedForReview", cancellationToken);

    public async Task<Result<TemplateFormRevisionDto>> RecordReviewAsync(
        Guid revisionId, TemplateFormTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = GuardTransition(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Review, TemplateFormRevisionStatus.InReview);
        if (guard is not null) return guard;
        if (revision!.CreatedById == actorId || revision.ReviewedById.HasValue)
            return TemplateFormErrors.SegregationOfDuties;
        revision.ReviewedById = actorId;
        revision.ReviewedAt = DateTime.UtcNow;
        AddAudit(revision, revision.Status, "ReviewCompleted",
            request.Reason, actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateFormRevisionDto>> ReturnToDraftAsync(
        Guid revisionId, TemplateFormTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = GuardTransition(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Review, TemplateFormRevisionStatus.InReview);
        if (guard is not null) return guard;
        if (revision!.CreatedById == actorId)
            return TemplateFormErrors.SegregationOfDuties;
        context.Entry(revision).Property(item => item.Status).OriginalValue =
            TemplateFormRevisionStatus.InReview;
        revision.Status = TemplateFormRevisionStatus.Draft;
        revision.ReviewedById = null;
        revision.ReviewedAt = null;
        AddAudit(revision, TemplateFormRevisionStatus.InReview, "ReturnedToDraft",
            request.Reason, actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateFormRevisionDto>> PublishAsync(
        Guid revisionId, TemplateFormTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken) : null;
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = GuardTransition(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Publish, TemplateFormRevisionStatus.InReview);
        if (guard is not null) return guard;
        if (!revision!.ReviewedById.HasValue || revision.CreatedById == actorId ||
            (revision.TemplateForm.TemplateArea.ReviewPolicyId == "regulated-three-person" &&
             revision.ReviewedById == actorId))
            return TemplateFormErrors.SegregationOfDuties;
        if (!await ReferencesArePublishedAsync(revision, cancellationToken))
            return TemplateFormErrors.DependencyUnavailable;
        var now = DateTime.UtcNow;
        var active = await RevisionQuery().SingleOrDefaultAsync(item =>
            item.TemplateFormId == revision.TemplateFormId &&
            item.Status == TemplateFormRevisionStatus.Published, cancellationToken);
        if (active is not null)
        {
            active.Status = TemplateFormRevisionStatus.Retired;
            active.RetiredAt = now;
            AddAudit(active, TemplateFormRevisionStatus.Published, "Superseded",
                request.Reason, actorId, correlationId);
        }
        revision.Status = TemplateFormRevisionStatus.Published;
        revision.PublishedById = actorId;
        revision.PublishedAt = now;
        AddAudit(revision, TemplateFormRevisionStatus.InReview, "Published",
            request.Reason, actorId, correlationId);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException) { return TemplateFormErrors.Conflict; }
        catch (DbUpdateException) { return TemplateFormErrors.Conflict; }
        context.ChangeTracker.Clear();
        var saved = await RevisionQuery().AsNoTracking()
            .SingleAsync(item => item.Id == revisionId, cancellationToken);
        return TemplateFormServiceSupport.ToDto(saved);
    }

    public async Task<Result<TemplateFormRevisionDto>> RetireAsync(
        Guid revisionId, TemplateFormTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = GuardTransition(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Publish, TemplateFormRevisionStatus.Published);
        if (guard is not null) return guard;
        if (revision!.CreatedById == actorId)
            return TemplateFormErrors.SegregationOfDuties;
        context.Entry(revision).Property(item => item.Status).OriginalValue =
            TemplateFormRevisionStatus.Published;
        revision.Status = TemplateFormRevisionStatus.Retired;
        revision.RetiredAt = DateTime.UtcNow;
        AddAudit(revision, TemplateFormRevisionStatus.Published, "Retired",
            request.Reason, actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    private async Task<Result<TemplateFormRevisionDto>> TransitionAsync(
        Guid revisionId, TemplateFormTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        TemplateDefinitionOperation operation, TemplateFormRevisionStatus expected,
        TemplateFormRevisionStatus next, string action, CancellationToken cancellationToken)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = GuardTransition(revision, request, actorRoleIds, operation, expected);
        if (guard is not null) return guard;
        context.Entry(revision!).Property(item => item.Status).OriginalValue = expected;
        revision!.Status = next;
        AddAudit(revision, expected, action, request.Reason, actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    private void AddAudit(
        TemplateFormRevision revision, TemplateFormRevisionStatus? prior,
        string action, string reason, Guid actorId, Guid correlationId)
    {
        var audit = TemplateFormServiceSupport.Audit(
            revision, prior, action, reason, actorId, correlationId);
        revision.Audits.Add(audit);
        context.Add(audit);
    }

    private static Error? GuardTransition(
        TemplateFormRevision? revision, TemplateFormTransitionRequest request,
        IReadOnlyCollection<Guid> actorRoleIds, TemplateDefinitionOperation operation,
        TemplateFormRevisionStatus expected)
    {
        if (revision is null) return TemplateFormErrors.NotFound;
        if (!revision.TemplateForm.TemplateArea.IsActive) return TemplateFormErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(
                revision.TemplateForm.TemplateArea, actorRoleIds, operation))
            return TemplateFormErrors.AccessDenied;
        return revision.Status != expected ||
               revision.ContentHash != request.ExpectedContentHash ||
               !TemplateFormServiceSupport.HasReason(request.Reason)
            ? TemplateFormErrors.Conflict : null;
    }
}
