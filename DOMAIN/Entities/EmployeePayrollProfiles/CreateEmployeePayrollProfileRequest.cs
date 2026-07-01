using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.EmployeePayrollProfiles;

public class CreateEmployeePayrollProfileRequest
{
    [Required] public Guid EmployeeId { get; set; }
    [Required] public Guid PayGroupId { get; set; }
    [Required] public Guid CurrencyId { get; set; }
    public PayrollPaymentMethod PaymentMethod { get; set; }

    [StringLength(20)] public string BankCode { get; set; }
    [StringLength(20)] public string BankBranchCode { get; set; }
    [StringLength(200)] public string BankAccountName { get; set; }

    [StringLength(50)] public string MobileMoneyProvider { get; set; }
    [StringLength(30)] public string MobileMoneyNumber { get; set; }

    public Guid? TaxProfileId { get; set; }
    public Guid? StatutoryProfileId { get; set; }

    [Range(0, double.MaxValue)] public decimal BaseSalary { get; set; }
    [StringLength(100)] public string CostCenter { get; set; }

    [Required] public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public bool IsExempt { get; set; }
}