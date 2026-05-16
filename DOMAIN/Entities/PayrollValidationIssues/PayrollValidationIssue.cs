using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.PayrollElements;
using DOMAIN.Entities.PayrollRuns;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.PayrollValidationIssues;


public class PayrollValidationIssue : BaseEntity
{
    public Guid PayrollRunId { get; set; }
    public PayrollRun PayrollRun { get; set; }

    public Guid? EmployeeId { get; set; }
    public Employee Employee { get; set; }

    public Guid? PayrollElementId { get; set; }
    public PayrollElement PayrollElement { get; set; }

    public PayrollValidationStage Stage { get; set; }
    public PayrollValidationSeverity Severity { get; set; }
    [StringLength(100)] public string Code { get; set; }
    [StringLength(2000)] public string Message { get; set; }
    [StringLength(int.MaxValue)] public string ContextJson { get; set; }

    public PayrollValidationIssueStatus Status { get; set; } = PayrollValidationIssueStatus.Open;

    public Guid? ResolvedById { get; set; }
    public User ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    [StringLength(2000)] public string ResolutionNotes { get; set; }
}

public enum PayrollValidationStage
{
    Precheck = 0,
    Calculation = 1,
    Validation = 2
}

public enum PayrollValidationSeverity
{
    Blocker = 0,
    Warning = 1
}

public enum PayrollValidationIssueStatus
{
    Open = 0,
    Acknowledged = 1,
    Resolved = 2,
    Suppressed = 3
}