using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

#nullable enable

namespace DOMAIN.Entities.FullProcedures;

public sealed class TemplateWorkflow : BaseEntity
{
    public Guid TemplateAreaId { get; set; }
    public TemplateArea TemplateArea { get; set; } = null!;
    public string PurposeId { get; set; } = string.Empty;
    public string SubjectTypeId { get; set; } = string.Empty;
    public List<TemplateWorkflowRevision> Revisions { get; set; } = [];
}

public sealed class TemplateWorkflowRevision : BaseEntity
{
    public Guid TemplateWorkflowId { get; set; }
    public TemplateWorkflow TemplateWorkflow { get; set; } = null!;
    public int Sequence { get; set; }
    public TemplateWorkflowRevisionStatus Status { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public Guid? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? PublishedById { get; set; }
    public User? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? RetiredAt { get; set; }
    public List<TemplateWorkflowNode> Nodes { get; set; } = [];
    public List<TemplateWorkflowEdge> Edges { get; set; } = [];
    public List<TemplateWorkflowNodeLayout> Layouts { get; set; } = [];
    public List<TemplateWorkflowRevisionAudit> Audits { get; set; } = [];
}

public enum TemplateWorkflowRevisionStatus { Draft = 0, InReview = 1, Published = 2, Retired = 3 }
