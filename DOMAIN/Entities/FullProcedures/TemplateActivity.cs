using DOMAIN.Entities.Base;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;

#nullable enable

namespace DOMAIN.Entities.FullProcedures;

public sealed class TemplateActivity : BaseEntity
{
    public Guid TemplateAreaId { get; set; }
    public TemplateArea TemplateArea { get; set; } = null!;
    public string PurposeId { get; set; } = string.Empty;
    public string SubjectTypeId { get; set; } = string.Empty;
    public List<TemplateActivityRevision> Revisions { get; set; } = [];
}

public sealed class TemplateActivityRevision : BaseEntity
{
    public Guid TemplateActivityId { get; set; }
    public TemplateActivity TemplateActivity { get; set; } = null!;
    public int Sequence { get; set; }
    public TemplateActivityRevisionStatus Status { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public Guid? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? PublishedById { get; set; }
    public User? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? RetiredAt { get; set; }
    public List<TemplateActivityFormBinding> Forms { get; set; } = [];
    public List<TemplateActivityAction> Actions { get; set; } = [];
    public List<TemplateActivityResourceRequirement> Resources { get; set; } = [];
    public List<TemplateActivityDataBinding> DataBindings { get; set; } = [];
    public List<TemplateActivityCompletionRule> CompletionRules { get; set; } = [];
    public List<TemplateActivityRevisionAudit> Audits { get; set; } = [];
}

public sealed class TemplateActivityFormBinding
{
    public Guid Id { get; set; }
    public Guid TemplateActivityRevisionId { get; set; }
    public TemplateActivityRevision TemplateActivityRevision { get; set; } = null!;
    public Guid TemplateFormId { get; set; }
    public Guid TemplateFormRevisionId { get; set; }
    public TemplateFormRevision TemplateFormRevision { get; set; } = null!;
    public string Key { get; set; } = string.Empty;
    public int Order { get; set; }
    public TemplateActivityFormUsage Usage { get; set; }
    public bool IsRequired { get; set; }
}

public sealed class TemplateActivityResourceRequirement
{
    public Guid Id { get; set; }
    public Guid TemplateActivityRevisionId { get; set; }
    public TemplateActivityRevision TemplateActivityRevision { get; set; } = null!;
    public string CapabilityId { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsRequired { get; set; }
}

public sealed class TemplateActivityDataBinding
{
    public Guid Id { get; set; }
    public Guid TemplateActivityRevisionId { get; set; }
    public TemplateActivityRevision TemplateActivityRevision { get; set; } = null!;
    public string Key { get; set; } = string.Empty;
    public int Order { get; set; }
    public TemplateActivityDataDirection Direction { get; set; }
    public TemplateActivityDataType DataType { get; set; }
    public bool IsRequired { get; set; }
}

public sealed class TemplateActivityCompletionRule
{
    public Guid Id { get; set; }
    public Guid TemplateActivityRevisionId { get; set; }
    public TemplateActivityRevision TemplateActivityRevision { get; set; } = null!;
    public int Order { get; set; }
    public TemplateActivityCompletionRuleType RuleType { get; set; }
    public string? TargetKey { get; set; }
}

public sealed class TemplateActivityRevisionAudit
{
    public Guid Id { get; set; }
    public Guid TemplateActivityRevisionId { get; set; }
    public TemplateActivityRevision TemplateActivityRevision { get; set; } = null!;
    public TemplateActivityRevisionStatus? PriorStatus { get; set; }
    public TemplateActivityRevisionStatus NewStatus { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public Guid ActorId { get; set; }
    public User Actor { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
    public Guid CorrelationId { get; set; }
}

public enum TemplateActivityRevisionStatus { Draft = 0, InReview = 1, Published = 2, Retired = 3 }
public enum TemplateActivityFormUsage { Input = 0, Output = 1, Evidence = 2 }
public enum TemplateActivityDataDirection { Input = 0, Output = 1 }
public enum TemplateActivityDataType { Text = 0, Number = 1, Boolean = 2, DateTime = 3, Document = 4, EntityReference = 5, Quantity = 6 }
public enum TemplateActivityCompletionRuleType { AllActionsCompleted = 0, FormSubmitted = 1, EvidenceCaptured = 2, ApprovalGranted = 3, DomainReceiptRecorded = 4 }
