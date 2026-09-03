namespace DOMAIN.Entities.Payroll;

public class CreateEmployeeTaxReliefRequest
{
    public Guid EmployeeId { get; set; }
    public TaxReliefType Type { get; set; }
    public decimal AnnualAmount { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class EmployeeTaxReliefDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public TaxReliefType Type { get; set; }
    public decimal AnnualAmount { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
