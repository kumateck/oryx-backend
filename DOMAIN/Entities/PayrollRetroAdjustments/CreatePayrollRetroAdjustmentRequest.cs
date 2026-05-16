using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.PayrollRetroAdjustments;

public class CreatePayrollRetroAdjustmentRequest
{
    [Required] public Guid PayrollCompanyId { get; set; }
    [Required] public Guid EmployeeId { get; set; }
    [Required] public Guid SourcePeriodId { get; set; }
    [Required] public PayrollRetroChangeType ChangeType { get; set; }
    [Required] [StringLength(2000)] public string Reason { get; set; }
    public string OldValueJson { get; set; }
    public string NewValueJson { get; set; }
}

public class MaterializeRetroAdjustmentRequest
{
    [Required] public Guid TargetRunId { get; set; }
}