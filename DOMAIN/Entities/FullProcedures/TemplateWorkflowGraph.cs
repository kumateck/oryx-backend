using DOMAIN.Entities.Users;

#nullable enable

namespace DOMAIN.Entities.FullProcedures;

public sealed class TemplateWorkflowNode
{
    public Guid Id { get; set; }
    public Guid TemplateWorkflowRevisionId { get; set; }
    public TemplateWorkflowRevision TemplateWorkflowRevision { get; set; } = null!;
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public TemplateWorkflowNodeType NodeType { get; set; }

    // Activity node: exact published Activity revision binding.
    public Guid? TemplateActivityId { get; set; }
    public Guid? TemplateActivityRevisionId { get; set; }
    public TemplateActivityRevision? TemplateActivityRevision { get; set; }

    // Fork/Join: the fork and its one matching join share this key.
    public string? JoinGroupKey { get; set; }

    // Wait/IPC: what the node is waiting on.
    public TemplateWorkflowWaitKind? WaitKind { get; set; }
    public string? WaitConfiguration { get; set; }

    // Hold/Resume: the hold and its resume point(s) share this key.
    public string? HoldGroupKey { get; set; }

    // Rework: the earlier node to return to, and the attempt ceiling that bounds the loop.
    public Guid? ReworkTargetNodeId { get; set; }
    public TemplateWorkflowNode? ReworkTargetNode { get; set; }
    public int? ReworkMaxAttempts { get; set; }

    public List<TemplateWorkflowEdge> OutgoingEdges { get; set; } = [];
    public List<TemplateWorkflowEdge> IncomingEdges { get; set; } = [];
}

public sealed class TemplateWorkflowEdge
{
    public Guid Id { get; set; }
    public Guid TemplateWorkflowRevisionId { get; set; }
    public TemplateWorkflowRevision TemplateWorkflowRevision { get; set; } = null!;
    public Guid SourceNodeId { get; set; }
    public TemplateWorkflowNode SourceNode { get; set; } = null!;
    public Guid TargetNodeId { get; set; }
    public TemplateWorkflowNode TargetNode { get; set; } = null!;
    public string? BranchKey { get; set; }
    public string? BranchExpression { get; set; }
    public int Order { get; set; }
}

public sealed class TemplateWorkflowNodeLayout
{
    public Guid Id { get; set; }
    public Guid TemplateWorkflowRevisionId { get; set; }
    public TemplateWorkflowRevision TemplateWorkflowRevision { get; set; } = null!;
    public Guid TemplateWorkflowNodeId { get; set; }
    public TemplateWorkflowNode TemplateWorkflowNode { get; set; } = null!;
    public double PositionX { get; set; }
    public double PositionY { get; set; }
}

public sealed class TemplateWorkflowRevisionAudit
{
    public Guid Id { get; set; }
    public Guid TemplateWorkflowRevisionId { get; set; }
    public TemplateWorkflowRevision TemplateWorkflowRevision { get; set; } = null!;
    public TemplateWorkflowRevisionStatus? PriorStatus { get; set; }
    public TemplateWorkflowRevisionStatus NewStatus { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public Guid ActorId { get; set; }
    public User Actor { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
    public Guid CorrelationId { get; set; }
}

public enum TemplateWorkflowNodeType
{
    Start = 0,
    End = 1,
    Activity = 2,
    Branch = 3,
    Fork = 4,
    Join = 5,
    Wait = 6,
    Ipc = 7,
    Hold = 8,
    Resume = 9,
    Rework = 10,
}

public enum TemplateWorkflowWaitKind { Duration = 0, ApprovedReceipt = 1, ExternalEvent = 2 }
