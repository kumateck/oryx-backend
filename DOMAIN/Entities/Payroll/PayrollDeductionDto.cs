namespace DOMAIN.Entities.Payroll;

public class CreatePayrollDeductionRequest
{
    public Guid EmployeeId { get; set; }
    public PayrollDeductionType Type { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class PayrollDeductionDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public PayrollDeductionType Type { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
