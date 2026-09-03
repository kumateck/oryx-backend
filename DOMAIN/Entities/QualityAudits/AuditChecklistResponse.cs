using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QualityAudits;

public class AuditChecklistResponse : BaseEntity
{
    public Guid QualityAuditId { get; set; }
    public QualityAudit QualityAudit { get; set; }

    // Null when the item is ad-hoc (not sourced from a template).
    public Guid? TemplateItemId { get; set; }
    public AuditChecklistTemplateItem TemplateItem { get; set; }
    public string AdHocQuestionText { get; set; }

    public ChecklistResponseStatus ResponseStatus { get; set; }
    public string Comments { get; set; }
    public Guid? RespondedById { get; set; }
    public User RespondedBy { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public enum ChecklistResponseStatus
{
    Compliant = 0,
    NonCompliant = 1,
    NotApplicable = 2,
    Observation = 3
}
