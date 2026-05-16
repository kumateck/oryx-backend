using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.EmployeePayrollProfiles;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.PayrollPaymentBatches;
using DOMAIN.Entities.PayrollRunEmployees;

namespace DOMAIN.Entities.PayrollPaymentInstructions;

public class PayrollPaymentInstruction : BaseEntity
{
    public Guid PayrollPaymentBatchId { get; set; }
    public PayrollPaymentBatch PayrollPaymentBatch { get; set; }

    public Guid PayrollRunEmployeeId { get; set; }
    public PayrollRunEmployee PayrollRunEmployee { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; }

    public PayrollPaymentMethod PaymentMethod { get; set; } = PayrollPaymentMethod.BankTransfer;

    [StringLength(20)] public string BankCode { get; set; }
    [StringLength(20)] public string BankBranchCode { get; set; }
    [StringLength(100)] public string BankAccountNumber { get; set; }
    [StringLength(200)] public string BankAccountName { get; set; }

    [StringLength(50)] public string MobileMoneyProvider { get; set; }
    [StringLength(30)] public string MobileMoneyNumber { get; set; }

    public decimal Amount { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; }

    [StringLength(100)] public string PaymentReference { get; set; }
    public PayrollPaymentInstructionStatus Status { get; set; } = PayrollPaymentInstructionStatus.Pending;

    [StringLength(int.MaxValue)] public string ProviderResponseJson { get; set; }
    public DateTime? SettledAt { get; set; }
    [StringLength(2000)] public string FailureReason { get; set; }
}

public enum PayrollPaymentInstructionStatus
{
    Pending = 0,
    Sent = 1,
    Confirmed = 2,
    Returned = 3,
    Failed = 4
}