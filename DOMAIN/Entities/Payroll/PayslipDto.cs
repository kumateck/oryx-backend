namespace DOMAIN.Entities.Payroll;

public class PayslipDto
{
    public Guid Id { get; set; }
    public Guid PayrollRunId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public string StaffNumber { get; set; }
    public string Department { get; set; }
    public string Designation { get; set; }
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
    public List<PayslipLineItemDto> LineItems { get; set; } = [];
}

public class PayslipLineItemDto
{
    public string Description { get; set; }
    public PayslipLineItemType Type { get; set; }
    public decimal Amount { get; set; }
}
