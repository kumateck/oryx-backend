using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Sites;
using DOMAIN.Entities.Users;

#nullable enable

namespace DOMAIN.Entities.FullProcedures;

public sealed class ProcedureDefinition : BaseEntity
{
    public Guid TemplateAreaId { get; set; }
    public TemplateArea TemplateArea { get; set; } = null!;
    public string PurposeId { get; set; } = string.Empty;
    public string SubjectTypeId { get; set; } = string.Empty;
    public List<ProcedureRevision> Revisions { get; set; } = [];
}

public sealed class ProcedureRevision : BaseEntity
{
    public Guid ProcedureDefinitionId { get; set; }
    public ProcedureDefinition ProcedureDefinition { get; set; } = null!;
    public int Sequence { get; set; }
    public ProcedureRevisionStatus Status { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid TemplateWorkflowId { get; set; }
    public Guid TemplateWorkflowRevisionId { get; set; }
    public TemplateWorkflowRevision TemplateWorkflowRevision { get; set; } = null!;
    public string TemplateWorkflowName { get; set; } = string.Empty;
    public string TemplateWorkflowContentHash { get; set; } = string.Empty;
    public string ParameterSchemaJson { get; set; } = "{}";
    public string ContentHash { get; set; } = string.Empty;
    public Guid? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RetiredAt { get; set; }
    public List<ProcedureApplicability> Applicabilities { get; set; } = [];
    public List<ProcedureStageScope> StageScopes { get; set; } = [];
    public List<ProcedureRevisionAudit> Audits { get; set; } = [];
}

public sealed class ProcedureApplicability
{
    public Guid Id { get; set; }
    public Guid ProcedureRevisionId { get; set; }
    public ProcedureRevision ProcedureRevision { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid SiteId { get; set; }
    public Site Site { get; set; } = null!;
    public ProcedureBatchType BatchType { get; set; }
}

public sealed class ProcedureStageScope
{
    public Guid Id { get; set; }
    public Guid ProcedureRevisionId { get; set; }
    public ProcedureRevision ProcedureRevision { get; set; } = null!;
    public Guid TemplateWorkflowRevisionId { get; set; }
    public Guid TemplateWorkflowNodeId { get; set; }
    public TemplateWorkflowNode TemplateWorkflowNode { get; set; } = null!;
    public string WorkflowNodeKey { get; set; } = string.Empty;
    public string WorkflowNodeName { get; set; } = string.Empty;
    public int WorkflowNodeOrder { get; set; }
    public ProcedureRecordScope RecordScope { get; set; }
}

public sealed class ProcedureRevisionAudit
{
    public Guid Id { get; set; }
    public Guid ProcedureRevisionId { get; set; }
    public ProcedureRevision ProcedureRevision { get; set; } = null!;
    public ProcedureRevisionStatus? PriorStatus { get; set; }
    public ProcedureRevisionStatus NewStatus { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public Guid ActorId { get; set; }
    public User Actor { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
    public Guid CorrelationId { get; set; }
}

public enum ProcedureRevisionStatus { Draft = 0, InReview = 1, Approved = 2, Retired = 3 }
public enum ProcedureBatchType { Trial = 0, Validation = 1, Commercial = 2, ScaleUp = 3 }
public enum ProcedureRecordScope { Manufacturing = 0, Packaging = 1, Shared = 2, Development = 3 }
