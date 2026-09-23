using System.Data;
using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable

namespace APP.Services.FullProcedures;

public sealed partial class TemplateSectionService
{
    public Task<Result<TemplateSectionRevisionDto>> SubmitForReviewAsync(
        Guid revisionId, TemplateSectionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default) => TransitionAsync(
            revisionId, request, actorId, actorRoleIds, correlationId,
            TemplateDefinitionOperation.Author, TemplateSectionRevisionStatus.Draft,
            TemplateSectionRevisionStatus.InReview, "SubmittedForReview", cancellationToken);

    public async Task<Result<TemplateSectionRevisionDto>> RecordReviewAsync(
        Guid revisionId, TemplateSectionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = GuardTransition(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Review, TemplateSectionRevisionStatus.InReview);
        if (guard is not null) return guard;
        if (revision!.CreatedById == actorId || revision.ReviewedById.HasValue)
            return TemplateSectionErrors.SegregationOfDuties;
        revision.ReviewedById = actorId;
        revision.ReviewedAt = DateTime.UtcNow;
        AddAudit(revision, revision.Status, "ReviewCompleted",
            request.Reason, actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateSectionRevisionDto>> ReturnToDraftAsync(
        Guid revisionId, TemplateSectionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = GuardTransition(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Review, TemplateSectionRevisionStatus.InReview);
        if (guard is not null) return guard;
        if (revision!.CreatedById == actorId)
            return TemplateSectionErrors.SegregationOfDuties;
        context.Entry(revision).Property(item => item.Status).OriginalValue =
            TemplateSectionRevisionStatus.InReview;
        revision.Status = TemplateSectionRevisionStatus.Draft;
        revision.ReviewedById = null;
        revision.ReviewedAt = null;
        AddAudit(revision, TemplateSectionRevisionStatus.InReview, "ReturnedToDraft",
            request.Reason, actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateSectionRevisionDto>> PublishAsync(
        Guid revisionId, TemplateSectionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken) : null;
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = GuardTransition(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Publish, TemplateSectionRevisionStatus.InReview);
        if (guard is not null) return guard;
        if (!revision!.ReviewedById.HasValue || revision.CreatedById == actorId ||
            (revision.TemplateSection.TemplateArea.ReviewPolicyId == "regulated-three-person" &&
             revision.ReviewedById == actorId))
            return TemplateSectionErrors.SegregationOfDuties;
        if (!await ReferencesArePublishedAsync(revision, cancellationToken))
            return TemplateSectionErrors.DependencyUnavailable;

        var now = DateTime.UtcNow;
        var active = await RevisionQuery().SingleOrDefaultAsync(item =>
            item.TemplateSectionId == revision.TemplateSectionId &&
            item.Status == TemplateSectionRevisionStatus.Published, cancellationToken);
        if (active is not null)
        {
            active.Status = TemplateSectionRevisionStatus.Retired;
            active.RetiredAt = now;
            AddAudit(active, TemplateSectionRevisionStatus.Published, "Superseded",
                request.Reason, actorId, correlationId);
        }
        revision.Status = TemplateSectionRevisionStatus.Published;
        revision.PublishedById = actorId;
        revision.PublishedAt = now;
        AddAudit(revision, TemplateSectionRevisionStatus.InReview, "Published",
            request.Reason, actorId, correlationId);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
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

    public async Task<Result<TemplateSectionRevisionDto>> RetireAsync(
        Guid revisionId, TemplateSectionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = GuardTransition(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Publish, TemplateSectionRevisionStatus.Published);
        if (guard is not null) return guard;
        if (revision!.CreatedById == actorId)
            return TemplateSectionErrors.SegregationOfDuties;
        context.Entry(revision).Property(item => item.Status).OriginalValue =
            TemplateSectionRevisionStatus.Published;
        revision.Status = TemplateSectionRevisionStatus.Retired;
        revision.RetiredAt = DateTime.UtcNow;
        AddAudit(revision, TemplateSectionRevisionStatus.Published, "Retired",
            request.Reason, actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    private async Task<Result<TemplateSectionRevisionDto>> TransitionAsync(
        Guid revisionId, TemplateSectionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        TemplateDefinitionOperation operation, TemplateSectionRevisionStatus expected,
        TemplateSectionRevisionStatus next, string action, CancellationToken cancellationToken)
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
        TemplateSectionRevision revision, TemplateSectionRevisionStatus? prior,
        string action, string reason, Guid actorId, Guid correlationId)
    {
        var audit = TemplateSectionServiceSupport.Audit(
            revision, prior, action, reason, actorId, correlationId);
        revision.Audits.Add(audit);
        context.Add(audit);
    }

    private static Error? GuardTransition(
        TemplateSectionRevision? revision, TemplateSectionTransitionRequest request,
        IReadOnlyCollection<Guid> actorRoleIds, TemplateDefinitionOperation operation,
        TemplateSectionRevisionStatus expected)
    {
        if (revision is null) return TemplateSectionErrors.NotFound;
        if (!revision.TemplateSection.TemplateArea.IsActive)
            return TemplateSectionErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(
                revision.TemplateSection.TemplateArea, actorRoleIds, operation))
            return TemplateSectionErrors.AccessDenied;
        return revision.Status != expected ||
               revision.ContentHash != request.ExpectedContentHash ||
               !TemplateSectionServiceSupport.HasReason(request.Reason)
            ? TemplateSectionErrors.Conflict : null;
    }
}
