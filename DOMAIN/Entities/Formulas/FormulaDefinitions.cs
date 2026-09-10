using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Formulas;

public class FormulaDefinition : BaseEntity
{
    public string Key { get; set; }
    public string Name { get; set; }
    public string PresentationPreset { get; set; }
    public List<FormulaRevision> Revisions { get; set; } = [];
}

public class QuestionFormulaDefinition : BaseEntity
{
    public Guid QuestionId { get; set; }
    public Question Question { get; set; }
    public Guid FormulaDefinitionId { get; set; }
    public FormulaDefinition FormulaDefinition { get; set; }
}

public class FormulaRevision : BaseEntity
{
    public Guid FormulaDefinitionId { get; set; }
    public FormulaDefinition FormulaDefinition { get; set; }
    public int Revision { get; set; }
    public string DefinitionJson { get; set; }
    public string TestCasesJson { get; set; }
    public string AuthoringPayloadJson { get; set; }
    public string AuthoringPayloadHash { get; set; }
    public string DefinitionHash { get; set; }
    public string ReleaseEvidenceHash { get; set; }
    public string FormulaLanguageVersion { get; set; }
    public string NumericPolicyVersion { get; set; }
    public FormulaRevisionStatus Status { get; set; }
    public Guid? ReviewedById { get; set; }
    public User ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ApprovedById { get; set; }
    public User ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? EffectiveAt { get; set; }
    public DateTime? RetiredAt { get; set; }
}

public class FormulaRevisionAudit
{
    public Guid Id { get; set; }
    public Guid FormulaRevisionId { get; set; }
    public FormulaRevision FormulaRevision { get; set; }
    public FormulaRevisionStatus? PriorStatus { get; set; }
    public FormulaRevisionStatus NewStatus { get; set; }
    public string Action { get; set; }
    public string Reason { get; set; }
    public string DefinitionHash { get; set; }
    public Guid ActorId { get; set; }
    public User Actor { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid CorrelationId { get; set; }
}
