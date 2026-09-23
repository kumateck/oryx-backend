namespace DOMAIN.Entities.FullProcedures;

#nullable enable

public class TemplateWorkflowContentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<TemplateWorkflowNodeRequest> Nodes { get; set; } = [];
    public List<TemplateWorkflowEdgeRequest> Edges { get; set; } = [];
    public List<TemplateWorkflowNodeLayoutRequest> Layouts { get; set; } = [];
}

public sealed class CreateTemplateWorkflowRequest : TemplateWorkflowContentRequest
{
    public Guid TemplateAreaId { get; set; }
    public string PurposeId { get; set; } = string.Empty;
    public string SubjectTypeId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class CreateTemplateWorkflowRevisionRequest : TemplateWorkflowContentRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class UpdateTemplateWorkflowRevisionRequest : TemplateWorkflowContentRequest
{
    public string ExpectedContentHash { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class UpdateTemplateWorkflowLayoutRequest
{
    public string ExpectedContentHash { get; set; } = string.Empty;
    public List<TemplateWorkflowNodeLayoutRequest> Layouts { get; set; } = [];
}

public sealed class TemplateWorkflowTransitionRequest
{
    public string ExpectedContentHash { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class TemplateWorkflowNodeRequest
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public TemplateWorkflowNodeType NodeType { get; set; }
    public Guid? TemplateActivityId { get; set; }
    public Guid? TemplateActivityRevisionId { get; set; }
    public string? JoinGroupKey { get; set; }
    public TemplateWorkflowWaitKind? WaitKind { get; set; }
    public string? WaitConfiguration { get; set; }
    public string? HoldGroupKey { get; set; }
    public string? ReworkTargetKey { get; set; }
    public int? ReworkMaxAttempts { get; set; }
}

public sealed class TemplateWorkflowEdgeRequest
{
    public string SourceKey { get; set; } = string.Empty;
    public string TargetKey { get; set; } = string.Empty;
    public string? BranchKey { get; set; }
    public string? BranchExpression { get; set; }
    public int Order { get; set; }
}

public sealed class TemplateWorkflowNodeLayoutRequest
{
    public string NodeKey { get; set; } = string.Empty;
    public double PositionX { get; set; }
    public double PositionY { get; set; }
}

public sealed record TemplateWorkflowDto(Guid Id, Guid TemplateAreaId, string AreaName,
    string PurposeId, string SubjectTypeId, TemplateWorkflowRevisionDto? LatestRevision);

public sealed record TemplateWorkflowDetailDto(Guid Id, Guid TemplateAreaId, string AreaName,
    string PurposeId, string SubjectTypeId, IReadOnlyList<TemplateWorkflowRevisionDto> Revisions);

public sealed record TemplateWorkflowRevisionDto(Guid Id, Guid TemplateWorkflowId, int Sequence,
    TemplateWorkflowRevisionStatus Status, string Name, string Description,
    IReadOnlyList<TemplateWorkflowNodeDto> Nodes, IReadOnlyList<TemplateWorkflowEdgeDto> Edges,
    IReadOnlyList<TemplateWorkflowNodeLayoutDto> Layouts, string ContentHash,
    Guid? CreatedById, Guid? ReviewedById, DateTime? ReviewedAt,
    Guid? PublishedById, DateTime? PublishedAt, DateTime? RetiredAt);

public sealed record TemplateWorkflowNodeDto(string Key, string Name, int Order,
    TemplateWorkflowNodeType NodeType, Guid? TemplateActivityId, Guid? TemplateActivityRevisionId,
    string? ActivityName, string? JoinGroupKey, TemplateWorkflowWaitKind? WaitKind,
    string? WaitConfiguration, string? HoldGroupKey, string? ReworkTargetKey,
    int? ReworkMaxAttempts);

public sealed record TemplateWorkflowEdgeDto(string SourceKey, string TargetKey,
    string? BranchKey, string? BranchExpression, int Order);

public sealed record TemplateWorkflowNodeLayoutDto(string NodeKey, double PositionX, double PositionY);
