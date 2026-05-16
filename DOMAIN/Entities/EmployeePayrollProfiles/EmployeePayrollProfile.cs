using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.PayGroups;
using DOMAIN.Entities.StatutoryProfiles;
using DOMAIN.Entities.TaxProfiles;

namespace DOMAIN.Entities.EmployeePayrollProfiles;

public class EmployeePayrollProfile : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; }

    public Guid PayGroupId { get; set; }
    public PayGroup PayGroup { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; }

    [StringLength(20)] public string BankCode { get; set; }
    [StringLength(20)] public string BankBranchCode { get; set; }
    [StringLength(200)] public string BankAccountName { get; set; }

    [StringLength(50)] public string MobileMoneyProvider { get; set; }
    [StringLength(30)] public string MobileMoneyNumber { get; set; }

    public Guid? TaxProfileId { get; set; }
    public TaxProfile TaxProfile { get; set; }

    public Guid? StatutoryProfileId { get; set; }
    public StatutoryProfile StatutoryProfile { get; set; }

    public decimal BaseSalary { get; set; }
    [StringLength(100)] public string CostCenter { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public bool IsExempt { get; set; }
}

public enum PayrollPaymentMethod
{
    BankTransfer = 0,
    Cash = 1,
    Mobile = 2,
    Cheque = 3
}
