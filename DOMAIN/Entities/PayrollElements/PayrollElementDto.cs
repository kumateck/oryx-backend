using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.PayrollElements;


public class PayrollElementDto : BaseDto
{
    public string Code { get; set; }
    public string Name { get; set; }
    public PayrollElementType Type { get; set; }
    public PayrollElementCategory Category { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsRecurring { get; set; }
    public bool AffectsGross { get; set; }
    public bool AffectsNet { get; set; }
    public Guid? DefaultAccountId { get; set; }
    public bool IsActive { get; set; }
    public CollectionItemDto PayrollCompany { get; set; }
    public List<PayrollElementVersionDto> Versions { get; set; } = [];
}

public class PayrollElementVersionDto : BaseDto
{
    public CollectionItemDto PayrollElement { get; set; }
    public int VersionNumber { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string FormulaExpression { get; set; }
    public decimal? FlatAmount { get; set; }
    public decimal? Rate { get; set; }
    public string BandsJson { get; set; }
    public PayrollElementVersionStatus Status { get; set; }
    public bool Approved { get; set; }
}