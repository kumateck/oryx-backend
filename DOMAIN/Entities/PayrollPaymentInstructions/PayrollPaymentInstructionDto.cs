using DOMAIN.Entities.Base;

using DOMAIN.Entities.EmployeePayrollProfiles;
using SHARED;

namespace DOMAIN.Entities.PayrollPaymentInstructions;

public class PayrollPaymentInstructionDto : BaseDto
{
    public CollectionItemDto PayrollPaymentBatch { get; set; }
    public CollectionItemDto PayrollRunEmployee { get; set; }
    public CollectionItemDto Employee { get; set; }
    
    public PayrollPaymentMethod PaymentMethod { get; set; }
    public string BankCode { get; set; }
    public string BankBranchCode { get; set; }
    public string BankAccountNumber { get; set; }
    public string BankAccountName { get; set; }
    
    public string MobileMoneyProvider { get; set; }
    public string MobileMoneyNumber { get; set; }

    public decimal Amount { get; set; }
    public CollectionItemDto Currency { get; set; }

    public string PaymentReference { get; set; }
    public PayrollPaymentInstructionStatus Status { get; set; }

    public string ProviderResponseJson { get; set; }
    public DateTime? SettledAt { get; set; }
    public string FailureReason { get; set; }
}
