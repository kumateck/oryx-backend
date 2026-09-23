using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FullProcedures;

public sealed partial class TemplateWorkflowService
{
    public async Task<Result<TemplateWorkflowRevisionDto>> CreateAsync(
        CreateTemplateWorkflowRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!TemplateWorkflowServiceSupport.HasReason(request.Reason))
            return TemplateWorkflowErrors.Invalid;
        var area = await LoadAreaAsync(request.TemplateAreaId, cancellationToken);
        if (area is null) return TemplateAreaErrors.NotFound;
        if (!area.IsActive) return TemplateWorkflowErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(
                area, actorRoleIds, TemplateDefinitionOperation.Author))
            return TemplateWorkflowErrors.AccessDenied;
        if (!ValidContext(area, request.PurposeId, request.SubjectTypeId))
            return TemplateWorkflowErrors.Invalid;
        var content = await ResolveContentAsync(request, area, request.PurposeId,
            request.SubjectTypeId, cancellationToken);
        if (content is null) return TemplateWorkflowErrors.Invalid;
        var workflow = new TemplateWorkflow
        {
            Id = Guid.NewGuid(), TemplateAreaId = area.Id,
            PurposeId = request.PurposeId, SubjectTypeId = request.SubjectTypeId,
            CreatedById = actorId,
        };
        var revision = NewRevision(workflow.Id, 1, actorId, content);
        revision.Audits.Add(TemplateWorkflowServiceSupport.Audit(revision, null,
            "DraftCreated", request.Reason, actorId, correlationId));
        workflow.Revisions.Add(revision);
        context.Add(workflow);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateWorkflowRevisionDto>> CreateRevisionAsync(
        Guid workflowId, CreateTemplateWorkflowRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!TemplateWorkflowServiceSupport.HasReason(request.Reason))
            return TemplateWorkflowErrors.Invalid;
        var workflow = await WorkflowQuery().SingleOrDefaultAsync(
            x => x.Id == workflowId, cancellationToken);
        if (workflow is null) return TemplateWorkflowErrors.NotFound;
        if (!workflow.TemplateArea.IsActive) return TemplateWorkflowErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(workflow.TemplateArea,
                actorRoleIds, TemplateDefinitionOperation.Author))
            return TemplateWorkflowErrors.AccessDenied;
        if (workflow.Revisions.Any(x => x.Status is
                TemplateWorkflowRevisionStatus.Draft or TemplateWorkflowRevisionStatus.InReview))
            return TemplateWorkflowErrors.Conflict;
        var content = await ResolveContentAsync(request, workflow.TemplateArea,
            workflow.PurposeId, workflow.SubjectTypeId, cancellationToken);
        if (content is null) return TemplateWorkflowErrors.Invalid;
        var sequence = workflow.Revisions.Select(x => x.Sequence).DefaultIfEmpty().Max() + 1;
        var revision = NewRevision(workflow.Id, sequence, actorId, content);
        revision.Audits.Add(TemplateWorkflowServiceSupport.Audit(revision, null,
            "DraftCreated", request.Reason, actorId, correlationId));
        context.Add(revision);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    public async Task<Result<TemplateWorkflowRevisionDto>> UpdateDraftAsync(Guid revisionId,
        UpdateTemplateWorkflowRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!TemplateWorkflowServiceSupport.HasReason(request.Reason))
            return TemplateWorkflowErrors.Invalid;
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        if (revision is null) return TemplateWorkflowErrors.NotFound;
        var workflow = revision.TemplateWorkflow;
        if (!workflow.TemplateArea.IsActive) return TemplateWorkflowErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(workflow.TemplateArea,
                actorRoleIds, TemplateDefinitionOperation.Author))
            return TemplateWorkflowErrors.AccessDenied;
        if (revision.Status != TemplateWorkflowRevisionStatus.Draft ||
            revision.ContentHash != request.ExpectedContentHash)
            return TemplateWorkflowErrors.Conflict;
        var content = await ResolveContentAsync(request, workflow.TemplateArea,
            workflow.PurposeId, workflow.SubjectTypeId, cancellationToken);
        if (content is null) return TemplateWorkflowErrors.Invalid;
        context.Entry(revision).Property(x => x.ContentHash).OriginalValue =
            request.ExpectedContentHash;
        context.RemoveRange(revision.Edges);
        context.RemoveRange(revision.Layouts);
        context.RemoveRange(revision.Nodes);
        TemplateWorkflowServiceSupport.Apply(revision, content);
        context.AddRange(revision.Nodes.Cast<object>()
            .Concat(revision.Edges).Concat(revision.Layouts));
        var audit = TemplateWorkflowServiceSupport.Audit(revision,
            TemplateWorkflowRevisionStatus.Draft, "DraftUpdated", request.Reason,
            actorId, correlationId);
        revision.Audits.Add(audit);
        context.Add(audit);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }

    // Layout is deliberately outside the Draft-only, hash-fenced content lifecycle: repositioning
    // nodes on the canvas does not change ContentHash, does not require Draft status, and is not
    // audited, since it carries no semantic meaning for the governed workflow definition.
    public async Task<Result<TemplateWorkflowRevisionDto>> UpdateLayoutAsync(Guid revisionId,
        UpdateTemplateWorkflowLayoutRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        if (revision is null) return TemplateWorkflowErrors.NotFound;
        var workflow = revision.TemplateWorkflow;
        if (!workflow.TemplateArea.IsActive) return TemplateWorkflowErrors.InactiveArea;
        if (!TemplateDefinitionAuthorization.Allows(workflow.TemplateArea,
                actorRoleIds, TemplateDefinitionOperation.Author))
            return TemplateWorkflowErrors.AccessDenied;
        if (revision.Status == TemplateWorkflowRevisionStatus.Retired ||
            revision.ContentHash != request.ExpectedContentHash)
            return TemplateWorkflowErrors.Conflict;
        var nodeIdsByKey = revision.Nodes.ToDictionary(x => x.Key, x => x.Id,
            StringComparer.OrdinalIgnoreCase);
        if (request.Layouts.Any(item => string.IsNullOrWhiteSpace(item.NodeKey) ||
                !nodeIdsByKey.ContainsKey(item.NodeKey.Trim()) ||
                !double.IsFinite(item.PositionX) || !double.IsFinite(item.PositionY)) ||
            request.Layouts.Select(item => item.NodeKey.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Layouts.Count)
            return TemplateWorkflowErrors.Invalid;
        context.RemoveRange(revision.Layouts);
        revision.Layouts = request.Layouts.Select(item => new TemplateWorkflowNodeLayout
        {
            Id = Guid.NewGuid(), TemplateWorkflowRevisionId = revision.Id,
            TemplateWorkflowNodeId = nodeIdsByKey[item.NodeKey.Trim()],
            PositionX = item.PositionX, PositionY = item.PositionY,
        }).ToList();
        context.AddRange(revision.Layouts);
        return await SaveRevisionAsync(revision.Id, cancellationToken);
    }
}
