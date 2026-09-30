using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DOMAIN.Entities.FullProcedures;

#nullable enable

namespace APP.Services.FullProcedures;

internal sealed record WorkflowNodeContent(string Key, string Name, int Order,
    TemplateWorkflowNodeType NodeType, Guid? ActivityId, Guid? ActivityRevisionId,
    string? ActivityName, string? JoinGroupKey, TemplateWorkflowWaitKind? WaitKind,
    string? WaitConfiguration, string? HoldGroupKey, string? ReworkTargetKey, int? ReworkMaxAttempts);
internal sealed record WorkflowEdgeContent(string SourceKey, string TargetKey,
    string? BranchKey, string? BranchExpression, int Order);
internal sealed record WorkflowLayoutContent(string NodeKey, double PositionX, double PositionY);
internal sealed record TemplateWorkflowContent(string Name, string Description,
    IReadOnlyList<WorkflowNodeContent> Nodes, IReadOnlyList<WorkflowEdgeContent> Edges,
    IReadOnlyList<WorkflowLayoutContent> Layouts);

internal static class TemplateWorkflowServiceSupport
{
    internal static TemplateWorkflowContent BuildContent(TemplateWorkflowContentRequest request,
        IReadOnlyDictionary<Guid, (Guid ActivityId, string Name)> activities)
    {
        var nodes = request.Nodes.OrderBy(item => item.Order).Select(item =>
        {
            string? activityName = null;
            if (item.TemplateActivityRevisionId.HasValue &&
                activities.TryGetValue(item.TemplateActivityRevisionId.Value, out var activity))
                activityName = activity.Name;
            return new WorkflowNodeContent(item.Key.Trim(), item.Name.Trim(), item.Order,
                item.NodeType, item.TemplateActivityId, item.TemplateActivityRevisionId, activityName,
                item.JoinGroupKey?.Trim(), item.WaitKind, item.WaitConfiguration?.Trim(),
                item.HoldGroupKey?.Trim(), item.ReworkTargetKey?.Trim(), item.ReworkMaxAttempts);
        }).ToArray();
        var edges = request.Edges
            .OrderBy(item => item.SourceKey.Trim(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Order)
            .ThenBy(item => item.TargetKey.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(item => new WorkflowEdgeContent(item.SourceKey.Trim(), item.TargetKey.Trim(),
                item.BranchKey?.Trim(), item.BranchExpression?.Trim(), item.Order)).ToArray();
        var layouts = request.Layouts.Select(item =>
            new WorkflowLayoutContent(item.NodeKey.Trim(), item.PositionX, item.PositionY)).ToArray();
        return new TemplateWorkflowContent(request.Name.Trim(), request.Description.Trim(),
            nodes, edges, layouts);
    }

    internal static void Apply(TemplateWorkflowRevision revision, TemplateWorkflowContent content)
    {
        revision.Name = content.Name;
        revision.Description = content.Description;
        var nodeIdsByKey = content.Nodes.ToDictionary(item => item.Key, _ => Guid.NewGuid(),
            StringComparer.OrdinalIgnoreCase);
        revision.Nodes = content.Nodes.Select(item => new TemplateWorkflowNode
        {
            Id = nodeIdsByKey[item.Key], TemplateWorkflowRevisionId = revision.Id,
            Key = item.Key, Name = item.Name, Order = item.Order, NodeType = item.NodeType,
            TemplateActivityId = item.ActivityId, TemplateActivityRevisionId = item.ActivityRevisionId,
            JoinGroupKey = item.JoinGroupKey, WaitKind = item.WaitKind,
            WaitConfiguration = item.WaitConfiguration, HoldGroupKey = item.HoldGroupKey,
            ReworkTargetNodeId = item.ReworkTargetKey is null ? null : nodeIdsByKey[item.ReworkTargetKey],
            ReworkMaxAttempts = item.ReworkMaxAttempts,
        }).ToList();
        revision.Edges = content.Edges.Select(item => new TemplateWorkflowEdge
        {
            Id = Guid.NewGuid(), TemplateWorkflowRevisionId = revision.Id,
            SourceNodeId = nodeIdsByKey[item.SourceKey], TargetNodeId = nodeIdsByKey[item.TargetKey],
            BranchKey = item.BranchKey, BranchExpression = item.BranchExpression, Order = item.Order,
        }).ToList();
        revision.Layouts = content.Layouts.Select(item => new TemplateWorkflowNodeLayout
        {
            Id = Guid.NewGuid(), TemplateWorkflowRevisionId = revision.Id,
            TemplateWorkflowNodeId = nodeIdsByKey[item.NodeKey],
            PositionX = item.PositionX, PositionY = item.PositionY,
        }).ToList();
        revision.ContentHash = Hash(SnapshotJson(revision.TemplateWorkflowId, content));
    }

    internal static TemplateWorkflowRevisionAudit Audit(TemplateWorkflowRevision revision,
        TemplateWorkflowRevisionStatus? prior, string action, string reason,
        Guid actorId, Guid correlationId) => new()
        {
            Id = Guid.NewGuid(), TemplateWorkflowRevisionId = revision.Id,
            PriorStatus = prior, NewStatus = revision.Status, Action = action,
            Reason = reason.Trim(), ContentHash = revision.ContentHash,
            SnapshotJson = SnapshotJson(revision), ActorId = actorId,
            OccurredAt = DateTime.UtcNow, CorrelationId = correlationId,
        };

    internal static bool HasReason(string? value) => value?.Trim().Length is >= 10 and <= 4000;

    internal static TemplateWorkflowRevisionDto ToDto(TemplateWorkflowRevision item) => new(
        item.Id, item.TemplateWorkflowId, item.Sequence, item.Status, item.Name, item.Description,
        item.Nodes.OrderBy(x => x.Order).Select(x => ToNodeDto(x, item.Nodes)).ToArray(),
        item.Edges.OrderBy(x => x.Order).Select(x => ToEdgeDto(x, item.Nodes)).ToArray(),
        item.Layouts.Select(x => ToLayoutDto(x, item.Nodes)).ToArray(),
        item.ContentHash, item.CreatedById, item.ReviewedById, item.ReviewedAt,
        item.PublishedById, item.PublishedAt, item.RetiredAt);

    private static TemplateWorkflowNodeDto ToNodeDto(TemplateWorkflowNode node,
        IReadOnlyCollection<TemplateWorkflowNode> nodes) => new(
        node.Key, node.Name, node.Order, node.NodeType, node.TemplateActivityId,
        node.TemplateActivityRevisionId, node.TemplateActivityRevision?.Name, node.JoinGroupKey,
        node.WaitKind, node.WaitConfiguration, node.HoldGroupKey,
        node.ReworkTargetNodeId.HasValue
            ? nodes.Single(x => x.Id == node.ReworkTargetNodeId).Key
            : null, node.ReworkMaxAttempts);

    private static TemplateWorkflowEdgeDto ToEdgeDto(TemplateWorkflowEdge edge,
        IReadOnlyCollection<TemplateWorkflowNode> nodes) => new(
        nodes.Single(x => x.Id == edge.SourceNodeId).Key,
        nodes.Single(x => x.Id == edge.TargetNodeId).Key,
        edge.BranchKey, edge.BranchExpression, edge.Order);

    private static TemplateWorkflowNodeLayoutDto ToLayoutDto(TemplateWorkflowNodeLayout layout,
        IReadOnlyCollection<TemplateWorkflowNode> nodes) => new(
        nodes.Single(x => x.Id == layout.TemplateWorkflowNodeId).Key,
        layout.PositionX, layout.PositionY);

    private static string SnapshotJson(TemplateWorkflowRevision revision)
    {
        var keyById = revision.Nodes.ToDictionary(item => item.Id, item => item.Key);
        var content = new TemplateWorkflowContent(revision.Name, revision.Description,
            revision.Nodes.Select(item => new WorkflowNodeContent(item.Key, item.Name, item.Order,
                item.NodeType, item.TemplateActivityId, item.TemplateActivityRevisionId,
                item.TemplateActivityRevision?.Name, item.JoinGroupKey, item.WaitKind,
                item.WaitConfiguration, item.HoldGroupKey,
                item.ReworkTargetNodeId.HasValue ? keyById[item.ReworkTargetNodeId.Value] : null,
                item.ReworkMaxAttempts)).ToArray(),
            revision.Edges.Select(item => new WorkflowEdgeContent(keyById[item.SourceNodeId],
                keyById[item.TargetNodeId], item.BranchKey, item.BranchExpression, item.Order))
                .ToArray(),
            []);
        return SnapshotJson(revision.TemplateWorkflowId, content);
    }

    // Layouts are deliberately excluded: repositioning a node on the canvas must not change the
    // ContentHash or require a new governed revision.
    private static string SnapshotJson(Guid workflowId, TemplateWorkflowContent content) =>
        JsonSerializer.Serialize(new { workflowId, content.Name, content.Description,
            nodes = content.Nodes.OrderBy(x => x.Order).Select(x => new
                { x.Key, x.Name, x.Order, x.NodeType, x.ActivityId, x.ActivityRevisionId, x.JoinGroupKey,
                  x.WaitKind, x.WaitConfiguration, x.HoldGroupKey, x.ReworkTargetKey,
                  x.ReworkMaxAttempts }),
            edges = content.Edges
                .OrderBy(x => x.SourceKey, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Order)
                .ThenBy(x => x.TargetKey, StringComparer.OrdinalIgnoreCase)
                .Select(x => new
                    { x.SourceKey, x.TargetKey, x.BranchKey, x.BranchExpression, x.Order }) },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    private static string Hash(string value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
