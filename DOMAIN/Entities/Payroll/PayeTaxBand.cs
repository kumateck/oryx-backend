using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Payroll;

public class PayeTaxBand : BaseEntity
{
    public decimal LowerBound { get; set; }
    public decimal? UpperBound { get; set; }
    public decimal Rate { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
