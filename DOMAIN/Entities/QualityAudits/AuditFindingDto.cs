using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QualityAudits;

public class AuditFindingDto : WithAttachment
{
    public Guid QualityAuditId { get; set; }
    public Guid? ChecklistResponseId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public FindingSeverity Severity { get; set; }
    public string AreaOrClause { get; set; }
    public UserDto RaisedBy { get; set; }
    public DateTime RaisedAt { get; set; }
    public FindingStatus Status { get; set; }
    public AuditCorrectiveActionDto CorrectiveAction { get; set; }
}

public class RaiseFindingRequest
{
    public Guid? ChecklistResponseId { get; set; }

    [Required]
    public string Title { get; set; }

    [Required]
    public string Description { get; set; }

    [Required]
    public FindingSeverity Severity { get; set; }

    public string AreaOrClause { get; set; }
}
