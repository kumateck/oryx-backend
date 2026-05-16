using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.PayrollRuns;

public class CreatePayrollRunRequest
{
    [Required] public Guid PayrollCompanyId { get; set; }
    [Required] public Guid PayrollPeriodId { get; set; }
    [Required] public Guid PayGroupId { get; set; }
    public PayrollRunType RunType { get; set; }
    public Guid? ParentRunId { get; set; }
    [StringLength(2000)] public string Notes { get; set; }
}

public class PayrollRunTransitionRequest
{
    [Required] public PayrollRunStatus TargetStatus {get; set;}
    [StringLength(2000)] public string Reason { get; set; }
}

public class CancelPayrollRunRequest
{
    [Required] [StringLength(2000)] public string Reason { get; set; }
}