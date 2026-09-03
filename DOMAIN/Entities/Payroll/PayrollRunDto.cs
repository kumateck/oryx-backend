namespace DOMAIN.Entities.Payroll;

public class CreatePayrollRunRequest
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

public class PayrollRunDto
{
    public Guid Id { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public PayrollRunStatus Status { get; set; }
    public bool Approved { get; set; }
    public int EmployeeCount { get; set; }
    public decimal TotalGrossPay { get; set; }
    public decimal TotalNetPay { get; set; }
    public decimal TotalPayeTax { get; set; }
    public decimal TotalSsnitEmployeeContribution { get; set; }
    public decimal TotalSsnitEmployerContribution { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum PayrollRunSortBy
{
    CreatedAt = 0,
    PeriodStart = 1
}
