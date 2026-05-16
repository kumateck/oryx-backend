using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.PayRollPeriods;
using DOMAIN.Entities.PayrollResultLines;
using DOMAIN.Entities.PayrollRuns;

namespace DOMAIN.Entities.PayrollRunEmployees;

public class PayrollRunEmployee : BaseEntity
{
    public Guid PayrollRunId { get; set; }
    public PayrollRun PayrollRun { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; }

    [StringLength(int.MaxValue)] public string EmployeePayrollProfileSnapshot { get; set; }

    public decimal BasePay { get; set; }
    public decimal Gross { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalTaxes { get; set; }
    public decimal Net { get; set; }
    public decimal EmployerCost { get; set; }

    public EmployeeStatus EmployeeStatusAtRun { get; set; }
    public bool IsExcluded { get; set; }
    [StringLength(500)] public string ExclusionReason { get; set; }

    public Guid? RetroSourcePeriodId { get; set; }
    public PayrollPeriod RetroSourcePeriod { get; set; }

    public List<PayrollResultLine> ResultLines { get; set; } = [];

}