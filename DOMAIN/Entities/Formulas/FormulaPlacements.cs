using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Formulas;

public class FormRevision : BaseEntity
{
    public Guid FormId { get; set; }
    public Form Form { get; set; }
    public int Sequence { get; set; }
    public FormRevisionStatus Status { get; set; }
    public string ContentHash { get; set; }
    public Guid? ReviewedById { get; set; }
    public User ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ApprovedById { get; set; }
    public User ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RetiredAt { get; set; }
    public List<FormFieldRevision> Fields { get; set; } = [];
}

public class FormRevisionAudit
{
    public Guid Id { get; set; }
    public Guid FormRevisionId { get; set; }
    public FormRevision FormRevision { get; set; }
    public FormRevisionStatus? PriorStatus { get; set; }
    public FormRevisionStatus NewStatus { get; set; }
    public string Action { get; set; }
    public string Reason { get; set; }
    public string ContentHash { get; set; }
    public Guid ActorId { get; set; }
    public User Actor { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid CorrelationId { get; set; }
}

public class FormFieldRevision : BaseEntity
{
    public Guid FormRevisionId { get; set; }
    public FormRevision FormRevision { get; set; }
    public string PlacementKey { get; set; }
    public Guid FormFieldId { get; set; }
    public FormField FormField { get; set; }
    public Guid QuestionId { get; set; }
    public Question Question { get; set; }
    public bool Required { get; set; }
    public int Rank { get; set; }
    public string Description { get; set; }
    public string FieldHash { get; set; }
    public FormFieldFormulaConfiguration FormulaConfiguration { get; set; }
}

public class FormFieldFormulaConfiguration : BaseEntity
{
    public Guid FormFieldRevisionId { get; set; }
    public FormFieldRevision FormFieldRevision { get; set; }
    public Guid FormulaRevisionId { get; set; }
    public FormulaRevision FormulaRevision { get; set; }
    public string BindingsJson { get; set; }
    public string ResultTargetsJson { get; set; }
    public string DisplayPolicyJson { get; set; }
    public string MethodReference { get; set; }
    public string ConfigurationHash { get; set; }
}
