using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FullProcedures;

public sealed partial class TemplateActivityService
{
    public async Task<Result<TemplateActivityRevisionDto>> CreateAsync(
        CreateTemplateActivityRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!TemplateActivityServiceSupport.HasReason(request.Reason))
            return TemplateActivityErrors.Invalid;
        var area = await LoadAreaAsync(request.TemplateAreaId, cancellationToken);
        if (area is null) return TemplateAreaErrors.NotFound;
        if (!area.IsActive) return TemplateActivityErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(
                area, actorRoleIds, TemplateDefinitionOperation.Author))
            return TemplateActivityErrors.AccessDenied;
        if (!ValidContext(area, request.PurposeId, request.SubjectTypeId))
            return TemplateActivityErrors.Invalid;
        var content = await ResolveContentAsync(request, area, request.PurposeId,
            request.SubjectTypeId, cancellationToken);
        if (content is null) return TemplateActivityErrors.Invalid;
        var activity = new TemplateActivity
        {
            Id = Guid.NewGuid(), TemplateAreaId = area.Id,
            PurposeId = request.PurposeId, SubjectTypeId = request.SubjectTypeId,
            CreatedById = actorId,
        };
        var revision = NewRevision(activity.Id, 1, actorId, content);
        revision.Audits.Add(TemplateActivityServiceSupport.Audit(revision, null,
            "DraftCreated", request.Reason, actorId, correlationId));
        activity.Revisions.Add(revision);
        context.Add(activity);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateActivityRevisionDto>> CreateRevisionAsync(
        Guid activityId, CreateTemplateActivityRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!TemplateActivityServiceSupport.HasReason(request.Reason))
            return TemplateActivityErrors.Invalid;
        var activity = await ActivityQuery().SingleOrDefaultAsync(
            x => x.Id == activityId, cancellationToken);
        if (activity is null) return TemplateActivityErrors.NotFound;
        if (!activity.TemplateArea.IsActive) return TemplateActivityErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(activity.TemplateArea,
                actorRoleIds, TemplateDefinitionOperation.Author))
            return TemplateActivityErrors.AccessDenied;
        if (activity.Revisions.Any(x => x.Status is
                TemplateActivityRevisionStatus.Draft or TemplateActivityRevisionStatus.InReview))
            return TemplateActivityErrors.Conflict;
        var content = await ResolveContentAsync(request, activity.TemplateArea,
            activity.PurposeId, activity.SubjectTypeId, cancellationToken);
        if (content is null) return TemplateActivityErrors.Invalid;
        var sequence = activity.Revisions.Select(x => x.Sequence).DefaultIfEmpty().Max() + 1;
        var revision = NewRevision(activity.Id, sequence, actorId, content);
        revision.Audits.Add(TemplateActivityServiceSupport.Audit(revision, null,
            "DraftCreated", request.Reason, actorId, correlationId));
        context.Add(revision);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateActivityRevisionDto>> UpdateDraftAsync(Guid revisionId,
        UpdateTemplateActivityRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!TemplateActivityServiceSupport.HasReason(request.Reason))
            return TemplateActivityErrors.Invalid;
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        if (revision is null) return TemplateActivityErrors.NotFound;
        var activity = revision.TemplateActivity;
        if (!activity.TemplateArea.IsActive) return TemplateActivityErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(activity.TemplateArea,
                actorRoleIds, TemplateDefinitionOperation.Author))
            return TemplateActivityErrors.AccessDenied;
        if (revision.Status != TemplateActivityRevisionStatus.Draft ||
            revision.ContentHash != request.ExpectedContentHash)
            return TemplateActivityErrors.Conflict;
        var content = await ResolveContentAsync(request, activity.TemplateArea,
            activity.PurposeId, activity.SubjectTypeId, cancellationToken);
        if (content is null) return TemplateActivityErrors.Invalid;
        context.Entry(revision).Property(x => x.ContentHash).OriginalValue = request.ExpectedContentHash;
        context.RemoveRange(revision.Actions.SelectMany(x => x.Roles));
        context.RemoveRange(revision.Forms.Cast<object>().Concat(revision.Actions)
            .Concat(revision.Resources).Concat(revision.DataBindings)
            .Concat(revision.CompletionRules));
        TemplateActivityServiceSupport.Apply(revision, content);
        context.AddRange(revision.Forms.Cast<object>().Concat(revision.Actions)
            .Concat(revision.Resources).Concat(revision.DataBindings)
            .Concat(revision.CompletionRules));
        var audit = TemplateActivityServiceSupport.Audit(revision,
            TemplateActivityRevisionStatus.Draft, "DraftUpdated", request.Reason,
            actorId, correlationId);
        revision.Audits.Add(audit);
        context.Add(audit);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }
}
