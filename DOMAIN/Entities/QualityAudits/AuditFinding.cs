using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QualityAudits;

public class AuditFinding : BaseEntity
{
    public Guid QualityAuditId { get; set; }
    public QualityAudit QualityAudit { get; set; }

    // Null when the finding is raised standalone rather than off a specific checklist answer.
    public Guid? ChecklistResponseId { get; set; }
    public AuditChecklistResponse ChecklistResponse { get; set; }

    public string Title { get; set; }
    public string Description { get; set; }
    public FindingSeverity Severity { get; set; }
    public string AreaOrClause { get; set; }

    public Guid? RaisedById { get; set; }
    public User RaisedBy { get; set; }
    public DateTime RaisedAt { get; set; }

    public FindingStatus Status { get; set; }
    public AuditCorrectiveAction CorrectiveAction { get; set; }
}

public enum FindingSeverity
{
    Critical = 0,
    Major = 1,
    Minor = 2,
    Observation = 3
}

public enum FindingStatus
{
    Open = 0,
    CapaRaised = 1,
    Closed = 2
}
