using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.PayrollValidationIssues;


public class PayrollValidationIssueDto : BaseDto
{
    public CollectionItemDto PayrollRun { get; set; }
    public CollectionItemDto Employee { get; set; }
    public CollectionItemDto PayrollElement { get; set; }

    public PayrollValidationStage Stage { get; set; }
    public PayrollValidationSeverity Severity { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }
    public string ContextJson { get; set; }

    public PayrollValidationIssueStatus Status { get; set; }
    public UserDto ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string ResolutionNotes { get; set; }
}

public class ResolveValidationIssueRequest
{
    public PayrollValidationIssueStatus Status { get; set; }
    public string ResolutionNotes { get; set; }
}