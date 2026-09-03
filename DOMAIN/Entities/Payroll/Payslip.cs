using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;

namespace DOMAIN.Entities.Payroll;

public class Payslip : BaseEntity
{
    public Guid PayrollRunId { get; set; }
    public PayrollRun PayrollRun { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; }

    public decimal BasicSalary { get; set; }
    public decimal TotalAllowances { get; set; }
    public decimal TotalAdditions { get; set; }
    public decimal GrossPay { get; set; }
    public decimal TotalReliefs { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal SsnitEmployeeContribution { get; set; }
    public decimal SsnitEmployerContribution { get; set; }
    public decimal Tier2Contribution { get; set; }
    public decimal PayeTax { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }

    public List<PayslipLineItem> LineItems { get; set; } = [];
}

public class PayslipLineItem : BaseEntity
{
    public Guid PayslipId { get; set; }
    public Payslip Payslip { get; set; }

    [StringLength(500)] public string Description { get; set; }
    public PayslipLineItemType Type { get; set; }
    public decimal Amount { get; set; }
}

public enum PayslipLineItemType
{
    Allowance,
    Addition,
    StatutoryDeduction,
    OtherDeduction,
    Tax,
    TaxRelief
}
