using System.Data;
using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable

namespace APP.Services.FullProcedures;

public sealed partial class ProcedureService
{
    public Task<Result<ProcedureRevisionDto>> SubmitForReviewAsync(Guid revisionId,
        ProcedureTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default) => TransitionAsync(revisionId, request,
            actorId, actorRoleIds, correlationId, TemplateDefinitionOperation.Author,
            ProcedureRevisionStatus.Draft, ProcedureRevisionStatus.InReview,
            "SubmittedForReview", cancellationToken);

    public async Task<Result<ProcedureRevisionDto>> RecordReviewAsync(Guid revisionId,
        ProcedureTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = Guard(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Review, ProcedureRevisionStatus.InReview);
        if (guard is not null) return guard;
        if (revision!.CreatedById == actorId || revision.ReviewedById.HasValue)
            return ProcedureErrors.SegregationOfDuties;
        revision.ReviewedById = actorId;
        revision.ReviewedAt = DateTime.UtcNow;
        AddAudit(revision, revision.Status, "ReviewCompleted", request.Reason,
            actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<ProcedureRevisionDto>> ReturnToDraftAsync(Guid revisionId,
        ProcedureTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = Guard(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Review, ProcedureRevisionStatus.InReview);
        if (guard is not null) return guard;
        if (revision!.CreatedById == actorId) return ProcedureErrors.SegregationOfDuties;
        context.Entry(revision).Property(x => x.Status).OriginalValue =
            ProcedureRevisionStatus.InReview;
        revision.Status = ProcedureRevisionStatus.Draft;
        revision.ReviewedById = null;
        revision.ReviewedAt = null;
        AddAudit(revision, ProcedureRevisionStatus.InReview, "ReturnedToDraft",
            request.Reason, actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<ProcedureRevisionDto>> ApproveAsync(Guid revisionId,
        ProcedureTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable,
                cancellationToken) : null;
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = Guard(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Publish, ProcedureRevisionStatus.InReview);
        if (guard is not null) return guard;
        if (!revision!.ReviewedById.HasValue || revision.CreatedById == actorId ||
            (revision.ProcedureDefinition.TemplateArea.ReviewPolicyId ==
             "regulated-three-person" && revision.ReviewedById == actorId))
            return ProcedureErrors.SegregationOfDuties;
        if (!await DependenciesAvailableAsync(revision, cancellationToken))
            return ProcedureErrors.DependencyUnavailable;
        var now = DateTime.UtcNow;
        var active = await RevisionQuery().SingleOrDefaultAsync(x =>
            x.ProcedureDefinitionId == revision.ProcedureDefinitionId &&
            x.Status == ProcedureRevisionStatus.Approved, cancellationToken);
        if (active is not null)
        {
            active.Status = ProcedureRevisionStatus.Retired;
            active.RetiredAt = now;
            AddAudit(active, ProcedureRevisionStatus.Approved, "Superseded",
                request.Reason, actorId, correlationId);
        }
        revision.Status = ProcedureRevisionStatus.Approved;
        revision.ApprovedById = actorId;
        revision.ApprovedAt = now;
        AddAudit(revision, ProcedureRevisionStatus.InReview, "Approved",
            request.Reason, actorId, correlationId);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException) { return ProcedureErrors.Conflict; }
        catch (DbUpdateException) { return ProcedureErrors.Conflict; }
        context.ChangeTracker.Clear();
        var saved = await RevisionQuery().AsNoTracking()
            .SingleAsync(x => x.Id == revisionId, cancellationToken);
        return ProcedureServiceSupport.ToDto(saved);
    }

    public async Task<Result<ProcedureRevisionDto>> RetireAsync(Guid revisionId,
        ProcedureTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        var guard = Guard(revision, request, actorRoleIds,
            TemplateDefinitionOperation.Publish, ProcedureRevisionStatus.Approved);
        if (guard is not null) return guard;
        if (revision!.CreatedById == actorId) return ProcedureErrors.SegregationOfDuties;
        context.Entry(revision).Property(x => x.Status).OriginalValue =
            ProcedureRevisionStatus.Approved;
        revision.Status = ProcedureRevisionStatus.Retired;
        revision.RetiredAt = DateTime.UtcNow;
        AddAudit(revision, ProcedureRevisionStatus.Approved, "Retired",
            request.Reason, actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    private async Task<Result<ProcedureRevisionDto>> TransitionAsync(Guid revisionId,
        ProcedureTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        TemplateDefinitionOperation operation, ProcedureRevisionStatus expected,
        ProcedureRevisionStatus next, string action, CancellationToken token)
    {
        var revision = await LoadRevisionAsync(revisionId, token);
        var guard = Guard(revision, request, actorRoleIds, operation, expected);
        if (guard is not null) return guard;
        context.Entry(revision!).Property(x => x.Status).OriginalValue = expected;
        revision!.Status = next;
        AddAudit(revision, expected, action, request.Reason, actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, token);
    }

    private void AddAudit(ProcedureRevision revision, ProcedureRevisionStatus? prior,
        string action, string reason, Guid actorId, Guid correlationId)
    {
        var audit = ProcedureServiceSupport.Audit(revision, prior, action, reason,
            actorId, correlationId);
        revision.Audits.Add(audit);
        context.Add(audit);
    }

    private static Error? Guard(ProcedureRevision? revision,
        ProcedureTransitionRequest request, IReadOnlyCollection<Guid> actorRoleIds,
        TemplateDefinitionOperation operation, ProcedureRevisionStatus expected)
    {
        if (revision is null) return ProcedureErrors.NotFound;
        if (!revision.ProcedureDefinition.TemplateArea.IsActive) return ProcedureErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(
                revision.ProcedureDefinition.TemplateArea, actorRoleIds, operation))
            return ProcedureErrors.AccessDenied;
        return revision.Status != expected || revision.ContentHash != request.ExpectedContentHash ||
               !ProcedureServiceSupport.HasReason(request.Reason)
            ? ProcedureErrors.Conflict : null;
    }
}
