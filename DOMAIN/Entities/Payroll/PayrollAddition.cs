using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;

namespace DOMAIN.Entities.Payroll;

public class PayrollAddition : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; }

    public PayrollAdditionType Type { get; set; }
    [StringLength(500)] public string Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsTaxable { get; set; } = true;
    public bool IsRecurring { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public enum PayrollAdditionType
{
    Bonus,
    Commission,
    Overtime,
    Arrears,
    Other
}
