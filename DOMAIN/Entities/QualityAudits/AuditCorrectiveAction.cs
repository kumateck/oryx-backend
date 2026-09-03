using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QualityAudits;

public class AuditCorrectiveAction : BaseEntity
{
    public Guid AuditFindingId { get; set; }
    public AuditFinding AuditFinding { get; set; }

    public string RootCauseAnalysis { get; set; }
    public string CorrectiveActions { get; set; }
    public string PreventiveActions { get; set; }

    public Guid ResponsiblePersonId { get; set; }
    public User ResponsiblePerson { get; set; }
    public DateTime DueDate { get; set; }
    public CapaStatus Status { get; set; }

    public string EffectivenessCheckNotes { get; set; }
    public Guid? EffectivenessVerifiedById { get; set; }
    public User EffectivenessVerifiedBy { get; set; }
    public DateTime? EffectivenessVerifiedAt { get; set; }

    public DateTime? ClosedAt { get; set; }
}

public enum CapaStatus
{
    Open = 0,
    InProgress = 1,
    PendingVerification = 2,
    Closed = 3,
    Overdue = 4
}
