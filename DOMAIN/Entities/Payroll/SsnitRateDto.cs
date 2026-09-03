namespace DOMAIN.Entities.Payroll;

public class CreateSsnitRateRequest
{
    public decimal EmployeeRate { get; set; }
    public decimal EmployerRate { get; set; }
    public decimal Tier2Rate { get; set; }
    public decimal InsurableEarningsCeiling { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class SsnitRateDto
{
    public Guid Id { get; set; }
    public decimal EmployeeRate { get; set; }
    public decimal EmployerRate { get; set; }
    public decimal Tier2Rate { get; set; }
    public decimal InsurableEarningsCeiling { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
