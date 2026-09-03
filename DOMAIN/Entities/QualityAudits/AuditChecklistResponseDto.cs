using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QualityAudits;

public class AuditChecklistResponseDto : WithAttachment
{
    public Guid QualityAuditId { get; set; }
    public Guid? TemplateItemId { get; set; }
    public string SectionName { get; set; }
    public string QuestionText { get; set; }
    public string AdHocQuestionText { get; set; }
    public ChecklistResponseStatus ResponseStatus { get; set; }
    public string Comments { get; set; }
    public UserDto RespondedBy { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public class RecordChecklistResponseRequest
{
    public Guid? TemplateItemId { get; set; }
    public string AdHocQuestionText { get; set; }

    [Required]
    public ChecklistResponseStatus ResponseStatus { get; set; }

    public string Comments { get; set; }
}
