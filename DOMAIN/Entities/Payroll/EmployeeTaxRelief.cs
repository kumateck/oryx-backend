using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;

namespace DOMAIN.Entities.Payroll;

/// <summary>
/// A GRA income tax relief (an annual amount that reduces taxable pay before PAYE bands
/// are applied). Amounts are entered by HR rather than auto-derived, since several GRA
/// reliefs are conditional (age, dependents, disability) in ways this system does not verify.
/// </summary>
public class EmployeeTaxRelief : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; }

    public TaxReliefType Type { get; set; }
    public decimal AnnualAmount { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public enum TaxReliefType
{
    MarriageOrResponsibility,
    ChildEducation,
    AgedDependent,
    OldAge,
    Disability,
    LifeInsurancePremium,
    Other
}
