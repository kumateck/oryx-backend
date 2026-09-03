namespace DOMAIN.Entities.Payroll;

public class CreatePayrollAdditionRequest
{
    public Guid EmployeeId { get; set; }
    public PayrollAdditionType Type { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsTaxable { get; set; } = true;
    public bool IsRecurring { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class PayrollAdditionDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public PayrollAdditionType Type { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
