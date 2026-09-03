using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Payroll;

public class SsnitRate : BaseEntity
{
    public decimal EmployeeRate { get; set; }
    public decimal EmployerRate { get; set; }
    public decimal Tier2Rate { get; set; }
    public decimal InsurableEarningsCeiling { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
