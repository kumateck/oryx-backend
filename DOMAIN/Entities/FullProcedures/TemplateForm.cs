using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

#nullable enable

namespace DOMAIN.Entities.FullProcedures;

public sealed class TemplateForm : BaseEntity
{
    public Guid TemplateAreaId { get; set; }
    public TemplateArea TemplateArea { get; set; } = null!;
    public string PurposeId { get; set; } = string.Empty;
    public string SubjectTypeId { get; set; } = string.Empty;
    public List<TemplateFormRevision> Revisions { get; set; } = [];
}

public sealed class TemplateFormRevision : BaseEntity
{
    public Guid TemplateFormId { get; set; }
    public TemplateForm TemplateForm { get; set; } = null!;
    public int Sequence { get; set; }
    public TemplateFormRevisionStatus Status { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresEvidence { get; set; }
    public bool RequiresSignature { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public Guid? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? PublishedById { get; set; }
    public User? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? RetiredAt { get; set; }
    public List<TemplateFormSection> Sections { get; set; } = [];
    public List<TemplateFormConditionalRule> ConditionalRules { get; set; } = [];
    public List<TemplateFormRevisionAudit> Audits { get; set; } = [];
}

public sealed class TemplateFormSection
{
    public Guid Id { get; set; }
    public Guid TemplateFormRevisionId { get; set; }
    public TemplateFormRevision TemplateFormRevision { get; set; } = null!;
    public Guid TemplateSectionId { get; set; }
    public TemplateSection TemplateSection { get; set; } = null!;
    public Guid TemplateSectionRevisionId { get; set; }
    public TemplateSectionRevision TemplateSectionRevision { get; set; } = null!;
    public int Order { get; set; }
    public bool IsRequired { get; set; }
}

public sealed class TemplateFormConditionalRule
{
    public Guid Id { get; set; }
    public Guid TemplateFormRevisionId { get; set; }
    public TemplateFormRevision TemplateFormRevision { get; set; } = null!;
    public Guid TargetFormSectionId { get; set; }
    public TemplateFormSection TargetFormSection { get; set; } = null!;
    public Guid SourceFormSectionId { get; set; }
    public TemplateFormSection SourceFormSection { get; set; } = null!;
    public Guid SourceQuestionId { get; set; }
    public Guid SourceQuestionRevisionId { get; set; }
    public TemplateFormConditionOperator Operator { get; set; }
    public string? ComparisonValue { get; set; }
}

public sealed class TemplateFormRevisionAudit
{
    public Guid Id { get; set; }
    public Guid TemplateFormRevisionId { get; set; }
    public TemplateFormRevision TemplateFormRevision { get; set; } = null!;
    public TemplateFormRevisionStatus? PriorStatus { get; set; }
    public TemplateFormRevisionStatus NewStatus { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public Guid ActorId { get; set; }
    public User Actor { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
    public Guid CorrelationId { get; set; }
}

public enum TemplateFormRevisionStatus { Draft = 0, InReview = 1, Published = 2, Retired = 3 }
public enum TemplateFormConditionOperator { Equals = 0, NotEquals = 1, IsAnswered = 2 }
