using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FullProcedures;

public sealed partial class TemplateFormService
{
    public async Task<Result<TemplateFormRevisionDto>> CreateAsync(
        CreateTemplateFormRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!TemplateFormServiceSupport.HasReason(request.Reason))
            return TemplateFormErrors.Invalid;
        var area = await LoadAreaAsync(request.TemplateAreaId, cancellationToken);
        if (area is null) return TemplateAreaErrors.NotFound;
        if (!area.IsActive) return TemplateFormErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(
                area, actorRoleIds, TemplateDefinitionOperation.Author))
            return TemplateFormErrors.AccessDenied;
        if (!ValidContext(area, request.PurposeId, request.SubjectTypeId))
            return TemplateFormErrors.Invalid;
        var content = await ResolveContentAsync(request, area.Id,
            request.PurposeId, request.SubjectTypeId, cancellationToken);
        if (content is null) return TemplateFormErrors.Invalid;
        var form = new TemplateForm
        {
            Id = Guid.NewGuid(), TemplateAreaId = area.Id,
            PurposeId = request.PurposeId, SubjectTypeId = request.SubjectTypeId,
            CreatedById = actorId,
        };
        var revision = NewRevision(form.Id, 1, actorId, content);
        revision.Audits.Add(TemplateFormServiceSupport.Audit(
            revision, null, "DraftCreated", request.Reason, actorId, correlationId));
        form.Revisions.Add(revision);
        context.Add(form);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateFormRevisionDto>> CreateRevisionAsync(
        Guid formId, CreateTemplateFormRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!TemplateFormServiceSupport.HasReason(request.Reason))
            return TemplateFormErrors.Invalid;
        var form = await FormQuery().SingleOrDefaultAsync(
            item => item.Id == formId, cancellationToken);
        if (form is null) return TemplateFormErrors.NotFound;
        if (!form.TemplateArea.IsActive) return TemplateFormErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(
                form.TemplateArea, actorRoleIds, TemplateDefinitionOperation.Author))
            return TemplateFormErrors.AccessDenied;
        if (form.Revisions.Any(item => item.Status is
                TemplateFormRevisionStatus.Draft or TemplateFormRevisionStatus.InReview))
            return TemplateFormErrors.Conflict;
        var content = await ResolveContentAsync(request, form.TemplateAreaId,
            form.PurposeId, form.SubjectTypeId, cancellationToken);
        if (content is null) return TemplateFormErrors.Invalid;
        var sequence = form.Revisions.Select(item => item.Sequence).DefaultIfEmpty().Max() + 1;
        var revision = NewRevision(form.Id, sequence, actorId, content);
        revision.Audits.Add(TemplateFormServiceSupport.Audit(
            revision, null, "DraftCreated", request.Reason, actorId, correlationId));
        context.Add(revision);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateFormRevisionDto>> UpdateDraftAsync(
        Guid revisionId, UpdateTemplateFormRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!TemplateFormServiceSupport.HasReason(request.Reason))
            return TemplateFormErrors.Invalid;
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        if (revision is null) return TemplateFormErrors.NotFound;
        var form = revision.TemplateForm;
        if (!form.TemplateArea.IsActive) return TemplateFormErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(
                form.TemplateArea, actorRoleIds, TemplateDefinitionOperation.Author))
            return TemplateFormErrors.AccessDenied;
        if (revision.Status != TemplateFormRevisionStatus.Draft ||
            revision.ContentHash != request.ExpectedContentHash)
            return TemplateFormErrors.Conflict;
        var content = await ResolveContentAsync(request, form.TemplateAreaId,
            form.PurposeId, form.SubjectTypeId, cancellationToken);
        if (content is null) return TemplateFormErrors.Invalid;
        context.Entry(revision).Property(item => item.ContentHash).OriginalValue =
            request.ExpectedContentHash;
        context.RemoveRange(revision.ConditionalRules);
        context.RemoveRange(revision.Sections);
        TemplateFormServiceSupport.Apply(revision, content);
        context.AddRange(revision.Sections.Cast<object>().Concat(revision.ConditionalRules));
        var audit = TemplateFormServiceSupport.Audit(revision,
            TemplateFormRevisionStatus.Draft, "DraftUpdated", request.Reason,
            actorId, correlationId);
        revision.Audits.Add(audit);
        context.Add(audit);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }
}
