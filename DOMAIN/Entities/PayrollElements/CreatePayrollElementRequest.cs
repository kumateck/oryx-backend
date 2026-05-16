using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.PayrollElements;

public class CreatePayrollElementRequest
{
    [Required] [StringLength(50)] public string Code { get; set; }
    [Required] [StringLength(255)] public string Name { get; set; }
    [Required] public PayrollElementType Type { get; set; }
    [Required] public PayrollElementCategory Category { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsRecurring { get; set; }
    public bool AffectsGross { get; set; }
    public bool AffectsNet { get; set; }
    public Guid? DefaultAccountId { get; set; }
    [Required] public Guid PayrollCompanyId { get; set; }
}

public class CreatePayrollElementVersionRequest
{
    [Required] public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    [StringLength(4000)] public string FormulaExpression { get; set; }
    public decimal? FlatAmount { get; set; }
    public decimal? Rate { get; set; }
    public string BandsJson { get; set; }
}