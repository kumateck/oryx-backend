using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FullProcedures;

public sealed partial class ProcedureService
{
    public async Task<Result<ProcedureRevisionDto>> CreateAsync(CreateProcedureRequest request,
        Guid actorId, IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!ProcedureServiceSupport.HasReason(request.Reason)) return ProcedureErrors.Invalid;
        var source = await WorkflowRevisionQuery().AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == request.TemplateWorkflowRevisionId &&
            x.TemplateWorkflowId == request.TemplateWorkflowId, cancellationToken);
        if (source is null) return ProcedureErrors.NotFound;
        var area = source.TemplateWorkflow.TemplateArea;
        if (!area.IsActive) return ProcedureErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(
                area, actorRoleIds, TemplateDefinitionOperation.Author))
            return ProcedureErrors.AccessDenied;
        var content = await ResolveContentAsync(request, area.Id,
            source.TemplateWorkflow.PurposeId, source.TemplateWorkflow.SubjectTypeId,
            cancellationToken);
        if (content is null) return ProcedureErrors.Invalid;
        var definition = new ProcedureDefinition
        {
            Id = Guid.NewGuid(), TemplateAreaId = area.Id, PurposeId = content.PurposeId,
            SubjectTypeId = content.SubjectTypeId, CreatedById = actorId,
        };
        var revision = NewRevision(definition, 1, actorId, content);
        revision.Audits.Add(ProcedureServiceSupport.Audit(revision, null,
            "DraftCreated", request.Reason, actorId, correlationId));
        definition.Revisions.Add(revision);
        context.Add(definition);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<ProcedureRevisionDto>> CreateRevisionAsync(Guid definitionId,
        CreateProcedureRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!ProcedureServiceSupport.HasReason(request.Reason)) return ProcedureErrors.Invalid;
        var definition = await DefinitionQuery().SingleOrDefaultAsync(
            x => x.Id == definitionId, cancellationToken);
        if (definition is null) return ProcedureErrors.NotFound;
        if (!definition.TemplateArea.IsActive) return ProcedureErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(definition.TemplateArea,
                actorRoleIds, TemplateDefinitionOperation.Author))
            return ProcedureErrors.AccessDenied;
        if (definition.Revisions.Any(x => x.Status is
                ProcedureRevisionStatus.Draft or ProcedureRevisionStatus.InReview))
            return ProcedureErrors.Conflict;
        var content = await ResolveContentAsync(request, definition.TemplateAreaId,
            definition.PurposeId, definition.SubjectTypeId, cancellationToken);
        if (content is null) return ProcedureErrors.Invalid;
        var sequence = definition.Revisions.Select(x => x.Sequence).DefaultIfEmpty().Max() + 1;
        var revision = NewRevision(definition, sequence, actorId, content);
        revision.Audits.Add(ProcedureServiceSupport.Audit(revision, null,
            "DraftCreated", request.Reason, actorId, correlationId));
        context.Add(revision);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<ProcedureRevisionDto>> UpdateDraftAsync(Guid revisionId,
        UpdateProcedureRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!ProcedureServiceSupport.HasReason(request.Reason)) return ProcedureErrors.Invalid;
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        if (revision is null) return ProcedureErrors.NotFound;
        var definition = revision.ProcedureDefinition;
        if (!definition.TemplateArea.IsActive) return ProcedureErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(definition.TemplateArea,
                actorRoleIds, TemplateDefinitionOperation.Author))
            return ProcedureErrors.AccessDenied;
        if (revision.Status != ProcedureRevisionStatus.Draft ||
            revision.ContentHash != request.ExpectedContentHash) return ProcedureErrors.Conflict;
        var content = await ResolveContentAsync(request, definition.TemplateAreaId,
            definition.PurposeId, definition.SubjectTypeId, cancellationToken);
        if (content is null) return ProcedureErrors.Invalid;
        context.Entry(revision).Property(x => x.ContentHash).OriginalValue =
            request.ExpectedContentHash;
        context.RemoveRange(revision.Applicabilities);
        context.RemoveRange(revision.StageScopes);
        ProcedureServiceSupport.Apply(revision, content);
        context.AddRange(revision.Applicabilities.Cast<object>().Concat(revision.StageScopes));
        AddAudit(revision, ProcedureRevisionStatus.Draft, "DraftUpdated", request.Reason,
            actorId, correlationId);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    private static ProcedureRevision NewRevision(ProcedureDefinition definition, int sequence,
        Guid actorId, ProcedureContent content)
    {
        var revision = new ProcedureRevision
        {
            Id = Guid.NewGuid(), ProcedureDefinitionId = definition.Id,
            ProcedureDefinition = definition, Sequence = sequence,
            Status = ProcedureRevisionStatus.Draft, CreatedById = actorId,
        };
        ProcedureServiceSupport.Apply(revision, content);
        return revision;
    }
}
