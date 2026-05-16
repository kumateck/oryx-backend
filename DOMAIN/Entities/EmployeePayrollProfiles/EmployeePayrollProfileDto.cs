using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.EmployeePayrollProfiles;

public class EmployeePayrollProfileDto : BaseDto
{
    public CollectionItemDto Employee { get; set; }
    public CollectionItemDto PayGroup { get; set; }
    public CollectionItemDto PayCurrency { get; set; }
    public PayrollPaymentMethod PaymentMethod { get; set; }

    public string BankCode { get; set; }
    public string BankBranchCode { get; set; }
    public string BankAccountName { get; set; }

    public string MobileMoneyProvider { get; set; }
    public string MobileMoneyNumber { get; set; }

    public CollectionItemDto TaxProfile { get; set; }
    public CollectionItemDto StatutoryProfile { get; set; }

    public decimal BaseSalary { get; set; }
    public string CostCenter { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public bool IsExempt { get; set; }
}