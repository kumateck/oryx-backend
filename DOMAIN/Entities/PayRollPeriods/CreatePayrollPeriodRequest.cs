using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.PayRollPeriods;

public class CreatePayrollPeriodRequest
{
    [Required, StringLength(50)] public string Code { get; set; }
    [Required] public DateTime PeriodStart { get; set; }
    [Required] public DateTime PeriodEnd { get; set; }
    [Required] public DateTime CutoffDate { get; set; }
    [Required] public DateTime PaymentDueDate { get; set; }
    [Required] public Guid PayrollCompanyId { get; set; }
    [Required] public Guid PayrollCalendarId { get; set; }
}

public class ClosePayrollPeriodRequest
{
    [Required] public Guid PeriodId { get; set; }
    [StringLength(2000)] public string Reason { get; set; }
}

public class ReopenPayrollPeriodRequest
{
    [Required] public Guid PeriodId { get; set; }
    [Required] [StringLength(2000)] public string Reason { get; set; }
}
