using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

#nullable enable

namespace DOMAIN.Entities.FullProcedures;

public sealed class TemplateQuestion : BaseEntity
{
    public Guid TemplateAreaId { get; set; }
    public TemplateArea TemplateArea { get; set; } = null!;
    public string PurposeId { get; set; } = string.Empty;
    public string SubjectTypeId { get; set; } = string.Empty;
    public List<TemplateQuestionRevision> Revisions { get; set; } = [];
}

public sealed class TemplateQuestionRevision : BaseEntity
{
    public Guid TemplateQuestionId { get; set; }
    public TemplateQuestion TemplateQuestion { get; set; } = null!;
    public int Sequence { get; set; }
    public TemplateQuestionRevisionStatus Status { get; set; }
    public string Wording { get; set; } = string.Empty;
    public TemplateQuestionAnswerType AnswerType { get; set; }
    public string InputType { get; set; } = string.Empty;
    public Guid? UnitOfMeasureId { get; set; }
    public UnitOfMeasure? UnitOfMeasure { get; set; }
    public bool Required { get; set; }
    public string? HelpText { get; set; }
    public decimal? Minimum { get; set; }
    public decimal? Maximum { get; set; }
    public TemplateQuestionSensitivity Sensitivity { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public Guid? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? PublishedById { get; set; }
    public User? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? RetiredAt { get; set; }
    public List<TemplateQuestionOption> Options { get; set; } = [];
    public List<TemplateQuestionCalculationReference> CalculationReferences { get; set; } = [];
    public List<TemplateQuestionRevisionAudit> Audits { get; set; } = [];
}

public sealed class TemplateQuestionOption
{
    public Guid Id { get; set; }
    public Guid TemplateQuestionRevisionId { get; set; }
    public TemplateQuestionRevision TemplateQuestionRevision { get; set; } = null!;
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Rank { get; set; }
}

public sealed class TemplateQuestionCalculationReference
{
    public Guid Id { get; set; }
    public Guid TemplateQuestionRevisionId { get; set; }
    public TemplateQuestionRevision TemplateQuestionRevision { get; set; } = null!;
    public Guid ReferencedQuestionId { get; set; }
    public TemplateQuestion ReferencedQuestion { get; set; } = null!;
    public Guid ReferencedQuestionRevisionId { get; set; }
    public TemplateQuestionRevision ReferencedQuestionRevision { get; set; } = null!;
}

public sealed class TemplateQuestionRevisionAudit
{
    public Guid Id { get; set; }
    public Guid TemplateQuestionRevisionId { get; set; }
    public TemplateQuestionRevision TemplateQuestionRevision { get; set; } = null!;
    public TemplateQuestionRevisionStatus? PriorStatus { get; set; }
    public TemplateQuestionRevisionStatus NewStatus { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public Guid ActorId { get; set; }
    public User Actor { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
    public Guid CorrelationId { get; set; }
}

public enum TemplateQuestionRevisionStatus
{
    Draft = 0,
    InReview = 1,
    Published = 2,
    Retired = 3,
}

public enum TemplateQuestionAnswerType
{
    ShortText = 0,
    LongText = 1,
    Number = 2,
    Date = 3,
    Time = 4,
    Month = 5,
    SingleChoice = 6,
    MultipleChoice = 7,
    Boolean = 8,
    Signature = 9,
    Image = 10,
    Table = 11,
    Calculation = 12,
}

public enum TemplateQuestionSensitivity
{
    Standard = 0,
    Confidential = 1,
    Restricted = 2,
}
