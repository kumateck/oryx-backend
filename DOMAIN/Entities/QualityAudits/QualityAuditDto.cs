using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QualityAudits;

public class QualityAuditDto : WithAttachment
{
    public string AuditNumber { get; set; }
    public AuditType Type { get; set; }
    public AuditFocus FocusArea { get; set; }
    public string Title { get; set; }
    public string Scope { get; set; }
    public string ObjectiveNotes { get; set; }
    public AuditStatus Status { get; set; }

    public DateTime ScheduledStartDate { get; set; }
    public DateTime ScheduledEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }

    public UserDto LeadAuditor { get; set; }
    public List<UserDto> TeamMembers { get; set; } = [];

    public Guid? ProductionOrderId { get; set; }
    public Guid? MaterialId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? SupplierId { get; set; }

    public Guid? ChecklistTemplateId { get; set; }
    public string ChecklistTemplateName { get; set; }

    public List<AuditChecklistResponseDto> ChecklistResponses { get; set; } = [];
    public List<AuditFindingDto> Findings { get; set; } = [];

    public string ClosingMeetingNotes { get; set; }
    public DateTime? ClosedAt { get; set; }
    public UserDto ClosedBy { get; set; }

    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public UserDto VerifiedBy { get; set; }
}

public class CreateQualityAuditRequest
{
    [Required]
    public AuditType Type { get; set; }

    [Required]
    public AuditFocus FocusArea { get; set; }

    [Required]
    public string Title { get; set; }

    [Required]
    public string Scope { get; set; }

    public string ObjectiveNotes { get; set; }

    [Required]
    public DateTime ScheduledStartDate { get; set; }

    [Required]
    public DateTime ScheduledEndDate { get; set; }

    [Required]
    public Guid LeadAuditorId { get; set; }

    public List<Guid> TeamMemberIds { get; set; } = [];

    public Guid? ProductionOrderId { get; set; }
    public Guid? MaterialId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? ChecklistTemplateId { get; set; }
}

public class UpdateQualityAuditRequest
{
    [Required]
    public string Title { get; set; }

    [Required]
    public string Scope { get; set; }

    public string ObjectiveNotes { get; set; }

    [Required]
    public DateTime ScheduledStartDate { get; set; }

    [Required]
    public DateTime ScheduledEndDate { get; set; }

    [Required]
    public Guid LeadAuditorId { get; set; }

    public List<Guid> TeamMemberIds { get; set; } = [];
}

public class SubmitAuditForClosureRequest
{
    public string ClosingMeetingNotes { get; set; }

    /// <summary>
    /// When true, allows submitting for closure with CAPAs still open, provided a justification is given.
    /// </summary>
    public bool AcceptOpenCapaDeferral { get; set; }
    public string DeferralJustification { get; set; }
}

public class CloseQualityAuditRequest
{
    [Required]
    public bool Approve { get; set; }
    public string Comments { get; set; }
}
