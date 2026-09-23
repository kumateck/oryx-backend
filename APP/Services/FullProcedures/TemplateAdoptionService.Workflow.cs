using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FullProcedures;

public sealed partial class TemplateAdoptionService
{
    private async Task<AdoptionDraftRequest> BuildWorkflowAsync(TemplateSharingGrant grant,
        AdoptTemplateRevisionRequest request, CancellationToken token)
    {
        var source = await context.Set<TemplateWorkflowRevision>().AsNoTracking().AsSplitQuery()
            .Include(x => x.Nodes).Include(x => x.Edges).Include(x => x.Layouts)
            .SingleOrDefaultAsync(x => x.Id == grant.RevisionId &&
                x.TemplateWorkflowId == grant.DefinitionId &&
                x.TemplateWorkflow.TemplateAreaId == grant.SourceAreaId &&
                x.Status == TemplateWorkflowRevisionStatus.Published &&
                x.ContentHash == grant.RevisionContentHash, token);
        if (source is null || request.RoleMappings.Count != 0) return null;
        var activityNodes = source.Nodes.Where(x => x.TemplateActivityRevisionId.HasValue).ToArray();
        var expected = activityNodes.Select(x =>
            (x.TemplateActivityId!.Value, x.TemplateActivityRevisionId!.Value)).Distinct().ToArray();
        if (!ExactDependencySet(request, expected) ||
            !await TargetActivitiesAvailableAsync(grant, request.DependencyMappings, token))
            return null;
        var nodeKeys = source.Nodes.ToDictionary(x => x.Id, x => x.Key);
        return new AdoptionDraftRequest(TemplateRevisionKind.Workflow,
            new CreateTemplateWorkflowRequest
            {
                TemplateAreaId = grant.TargetAreaId, PurposeId = grant.PurposeId,
                SubjectTypeId = grant.SubjectTypeId, Reason = request.Reason,
                Name = source.Name, Description = source.Description,
                Nodes = source.Nodes.OrderBy(x => x.Order).Select(x =>
                {
                    var map = x.TemplateActivityRevisionId.HasValue
                        ? FindDependency(request, x.TemplateActivityId!.Value,
                            x.TemplateActivityRevisionId.Value) : null;
                    return new TemplateWorkflowNodeRequest
                    {
                        Key = x.Key, Name = x.Name, Order = x.Order, NodeType = x.NodeType,
                        TemplateActivityId = map?.TargetDefinitionId,
                        TemplateActivityRevisionId = map?.TargetRevisionId,
                        JoinGroupKey = x.JoinGroupKey, WaitKind = x.WaitKind,
                        WaitConfiguration = x.WaitConfiguration, HoldGroupKey = x.HoldGroupKey,
                        ReworkTargetKey = x.ReworkTargetNodeId.HasValue
                            ? nodeKeys[x.ReworkTargetNodeId.Value] : null,
                        ReworkMaxAttempts = x.ReworkMaxAttempts,
                    };
                }).ToList(),
                Edges = source.Edges.OrderBy(x => x.Order).Select(x =>
                    new TemplateWorkflowEdgeRequest
                    {
                        SourceKey = nodeKeys[x.SourceNodeId], TargetKey = nodeKeys[x.TargetNodeId],
                        BranchKey = x.BranchKey, BranchExpression = x.BranchExpression,
                        Order = x.Order,
                    }).ToList(),
                Layouts = source.Layouts.Select(x => new TemplateWorkflowNodeLayoutRequest
                {
                    NodeKey = nodeKeys[x.TemplateWorkflowNodeId],
                    PositionX = x.PositionX, PositionY = x.PositionY,
                }).ToList(),
            });
    }

    private async Task<bool> TargetActivitiesAvailableAsync(TemplateSharingGrant grant,
        IReadOnlyCollection<TemplateAdoptionDependencyMappingRequest> mappings,
        CancellationToken token)
    {
        if (mappings.Count == 0) return true;
        var revisionIds = mappings.Select(x => x.TargetRevisionId).ToArray();
        var pairs = await context.Set<TemplateActivityRevision>().AsNoTracking()
            .Where(x => revisionIds.Contains(x.Id) &&
                x.Status == TemplateActivityRevisionStatus.Published &&
                x.TemplateActivity.TemplateAreaId == grant.TargetAreaId &&
                x.TemplateActivity.PurposeId == grant.PurposeId &&
                x.TemplateActivity.SubjectTypeId == grant.SubjectTypeId)
            .Select(x => new { x.TemplateActivityId, x.Id }).ToListAsync(token);
        var set = pairs.Select(x => (x.TemplateActivityId, x.Id)).ToHashSet();
        return set.Count == mappings.Count && mappings.All(x =>
            set.Contains((x.TargetDefinitionId, x.TargetRevisionId)));
    }

    private static Result<AdoptedDraft> From(Result<TemplateQuestionRevisionDto> result) =>
        result.IsSuccess ? new AdoptedDraft(result.Value.TemplateQuestionId,
            result.Value.Id, result.Value.ContentHash) : TemplateAdoptionErrors.DependencyUnavailable;
    private static Result<AdoptedDraft> From(Result<TemplateSectionRevisionDto> result) =>
        result.IsSuccess ? new AdoptedDraft(result.Value.TemplateSectionId,
            result.Value.Id, result.Value.ContentHash) : TemplateAdoptionErrors.DependencyUnavailable;
    private static Result<AdoptedDraft> From(Result<TemplateFormRevisionDto> result) =>
        result.IsSuccess ? new AdoptedDraft(result.Value.TemplateFormId,
            result.Value.Id, result.Value.ContentHash) : TemplateAdoptionErrors.DependencyUnavailable;
    private static Result<AdoptedDraft> From(Result<TemplateActivityRevisionDto> result) =>
        result.IsSuccess ? new AdoptedDraft(result.Value.TemplateActivityId,
            result.Value.Id, result.Value.ContentHash) : TemplateAdoptionErrors.DependencyUnavailable;
    private static Result<AdoptedDraft> From(Result<TemplateWorkflowRevisionDto> result) =>
        result.IsSuccess ? new AdoptedDraft(result.Value.TemplateWorkflowId,
            result.Value.Id, result.Value.ContentHash) : TemplateAdoptionErrors.DependencyUnavailable;
}
