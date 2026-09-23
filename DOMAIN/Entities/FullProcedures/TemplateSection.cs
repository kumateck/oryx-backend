using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

#nullable enable

namespace DOMAIN.Entities.FullProcedures;

public sealed class TemplateSection : BaseEntity
{
    public Guid TemplateAreaId { get; set; }
    public TemplateArea TemplateArea { get; set; } = null!;
    public string PurposeId { get; set; } = string.Empty;
    public string SubjectTypeId { get; set; } = string.Empty;
    public List<TemplateSectionRevision> Revisions { get; set; } = [];
}

public sealed class TemplateSectionRevision : BaseEntity
{
    public Guid TemplateSectionId { get; set; }
    public TemplateSection TemplateSection { get; set; } = null!;
    public int Sequence { get; set; }
    public TemplateSectionRevisionStatus Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public Guid? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? PublishedById { get; set; }
    public User? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? RetiredAt { get; set; }
    public List<TemplateSectionQuestion> Questions { get; set; } = [];
    public List<TemplateSectionConditionalRule> ConditionalRules { get; set; } = [];
    public List<TemplateSectionRevisionAudit> Audits { get; set; } = [];
}

public sealed class TemplateSectionQuestion
{
    public Guid Id { get; set; }
    public Guid TemplateSectionRevisionId { get; set; }
    public TemplateSectionRevision TemplateSectionRevision { get; set; } = null!;
    public Guid TemplateQuestionId { get; set; }
    public TemplateQuestion TemplateQuestion { get; set; } = null!;
    public Guid TemplateQuestionRevisionId { get; set; }
    public TemplateQuestionRevision TemplateQuestionRevision { get; set; } = null!;
    public int Order { get; set; }
}

public sealed class TemplateSectionConditionalRule
{
    public Guid Id { get; set; }
    public Guid TemplateSectionRevisionId { get; set; }
    public TemplateSectionRevision TemplateSectionRevision { get; set; } = null!;
    public Guid TargetSectionQuestionId { get; set; }
    public TemplateSectionQuestion TargetSectionQuestion { get; set; } = null!;
    public Guid DependsOnSectionQuestionId { get; set; }
    public TemplateSectionQuestion DependsOnSectionQuestion { get; set; } = null!;
    public TemplateSectionConditionOperator Operator { get; set; }
    public string? ComparisonValue { get; set; }
}

public sealed class TemplateSectionRevisionAudit
{
    public Guid Id { get; set; }
    public Guid TemplateSectionRevisionId { get; set; }
    public TemplateSectionRevision TemplateSectionRevision { get; set; } = null!;
    public TemplateSectionRevisionStatus? PriorStatus { get; set; }
    public TemplateSectionRevisionStatus NewStatus { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public Guid ActorId { get; set; }
    public User Actor { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
    public Guid CorrelationId { get; set; }
}

public enum TemplateSectionRevisionStatus
{
    Draft = 0,
    InReview = 1,
    Published = 2,
    Retired = 3,
}

public enum TemplateSectionConditionOperator
{
    Equals = 0,
    NotEquals = 1,
    IsAnswered = 2,
}
