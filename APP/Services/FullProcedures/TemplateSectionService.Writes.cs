using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FullProcedures;

public sealed partial class TemplateSectionService
{
    public async Task<Result<TemplateSectionRevisionDto>> CreateAsync(
        CreateTemplateSectionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!TemplateSectionServiceSupport.HasReason(request.Reason))
            return TemplateSectionErrors.Invalid;
        var area = await LoadAreaAsync(request.TemplateAreaId, cancellationToken);
        if (area is null) return TemplateAreaErrors.NotFound;
        if (!area.IsActive) return TemplateSectionErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(
                area, actorRoleIds, TemplateDefinitionOperation.Author))
            return TemplateSectionErrors.AccessDenied;
        if (!ValidContext(area, request.PurposeId, request.SubjectTypeId))
            return TemplateSectionErrors.Invalid;
        var content = await ResolveContentAsync(request, area.Id,
            request.PurposeId, request.SubjectTypeId, cancellationToken);
        if (content is null) return TemplateSectionErrors.Invalid;

        var section = new TemplateSection
        {
            Id = Guid.NewGuid(), TemplateAreaId = area.Id,
            PurposeId = request.PurposeId, SubjectTypeId = request.SubjectTypeId,
            CreatedById = actorId,
        };
        var revision = NewRevision(section.Id, 1, actorId, content);
        var audit = TemplateSectionServiceSupport.Audit(
            revision, null, "DraftCreated", request.Reason, actorId, correlationId);
        revision.Audits.Add(audit);
        section.Revisions.Add(revision);
        context.Add(section);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateSectionRevisionDto>> CreateRevisionAsync(
        Guid sectionId, CreateTemplateSectionRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!TemplateSectionServiceSupport.HasReason(request.Reason))
            return TemplateSectionErrors.Invalid;
        var section = await SectionQuery().SingleOrDefaultAsync(
            item => item.Id == sectionId, cancellationToken);
        if (section is null) return TemplateSectionErrors.NotFound;
        if (!section.TemplateArea.IsActive) return TemplateSectionErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(
                section.TemplateArea, actorRoleIds, TemplateDefinitionOperation.Author))
            return TemplateSectionErrors.AccessDenied;
        if (section.Revisions.Any(item => item.Status is
                TemplateSectionRevisionStatus.Draft or TemplateSectionRevisionStatus.InReview))
            return TemplateSectionErrors.Conflict;
        var content = await ResolveContentAsync(request, section.TemplateAreaId,
            section.PurposeId, section.SubjectTypeId, cancellationToken);
        if (content is null) return TemplateSectionErrors.Invalid;
        var sequence = section.Revisions.Select(item => item.Sequence).DefaultIfEmpty().Max() + 1;
        var revision = NewRevision(section.Id, sequence, actorId, content);
        var audit = TemplateSectionServiceSupport.Audit(
            revision, null, "DraftCreated", request.Reason, actorId, correlationId);
        revision.Audits.Add(audit);
        context.Add(revision);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateSectionRevisionDto>> UpdateDraftAsync(
        Guid revisionId, UpdateTemplateSectionRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!TemplateSectionServiceSupport.HasReason(request.Reason))
            return TemplateSectionErrors.Invalid;
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        if (revision is null) return TemplateSectionErrors.NotFound;
        var section = revision.TemplateSection;
        if (!section.TemplateArea.IsActive) return TemplateSectionErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(
                section.TemplateArea, actorRoleIds, TemplateDefinitionOperation.Author))
            return TemplateSectionErrors.AccessDenied;
        if (revision.Status != TemplateSectionRevisionStatus.Draft ||
            revision.ContentHash != request.ExpectedContentHash)
            return TemplateSectionErrors.Conflict;
        var content = await ResolveContentAsync(request, section.TemplateAreaId,
            section.PurposeId, section.SubjectTypeId, cancellationToken);
        if (content is null) return TemplateSectionErrors.Invalid;

        context.Entry(revision).Property(item => item.ContentHash).OriginalValue =
            request.ExpectedContentHash;
        context.RemoveRange(revision.ConditionalRules);
        context.RemoveRange(revision.Questions);
        TemplateSectionServiceSupport.Apply(revision, content);
        context.AddRange(revision.Questions.Cast<object>().Concat(revision.ConditionalRules));
        var audit = TemplateSectionServiceSupport.Audit(
            revision, TemplateSectionRevisionStatus.Draft, "DraftUpdated",
            request.Reason, actorId, correlationId);
        revision.Audits.Add(audit);
        context.Add(audit);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }
}
