using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;

namespace DOMAIN.Entities.Payroll;

public class EmployeeCompensation : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; }

    public Guid? PayGradeId { get; set; }
    public PayGrade PayGrade { get; set; }

    public decimal BasicSalary { get; set; }

    [StringLength(200)] public string BankName { get; set; }
    [StringLength(200)] public string BankBranch { get; set; }
    [StringLength(100)] public string BankAccountNumber { get; set; }
    [StringLength(20)] public string TinNumber { get; set; }

    public bool Tier3VoluntaryEnrolled { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public List<CompensationAllowance> Allowances { get; set; } = [];
}

public class CompensationAllowance : BaseEntity
{
    public Guid EmployeeCompensationId { get; set; }
    public EmployeeCompensation EmployeeCompensation { get; set; }

    public AllowanceType Type { get; set; }
    [StringLength(200)] public string Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsTaxable { get; set; } = true;
}

public enum AllowanceType
{
    Transport,
    Housing,
    Utility,
    Responsibility,
    Other
}
