using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QualityAudits;

public class AuditCorrectiveActionDto
{
    public Guid Id { get; set; }
    public Guid AuditFindingId { get; set; }
    public string RootCauseAnalysis { get; set; }
    public string CorrectiveActions { get; set; }
    public string PreventiveActions { get; set; }
    public UserDto ResponsiblePerson { get; set; }
    public DateTime DueDate { get; set; }
    public CapaStatus Status { get; set; }
    public string EffectivenessCheckNotes { get; set; }
    public UserDto EffectivenessVerifiedBy { get; set; }
    public DateTime? EffectivenessVerifiedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public class RaiseCorrectiveActionRequest
{
    [Required]
    public string RootCauseAnalysis { get; set; }

    [Required]
    public string CorrectiveActions { get; set; }

    public string PreventiveActions { get; set; }

    [Required]
    public Guid ResponsiblePersonId { get; set; }

    [Required]
    public DateTime DueDate { get; set; }
}

public class UpdateCorrectiveActionStatusRequest
{
    [Required]
    public CapaStatus Status { get; set; }
}

public class VerifyCorrectiveActionEffectivenessRequest
{
    [Required]
    public bool Effective { get; set; }
    public string EffectivenessCheckNotes { get; set; }
}
