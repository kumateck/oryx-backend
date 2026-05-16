using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.PayrollPaymentInstructions;
using DOMAIN.Entities.PayrollRuns;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.PayrollPaymentBatches;

public class PayrollPaymentBatch : BaseEntity, IRequireApproval
{
    [StringLength(50)] public string Code { get; set; }

    public Guid PayrollRunId { get; set; }
    public PayrollRun PayrollRun { get; set; }

    public PayrollPaymentBatchStatus BatchStatus { get; set; } = PayrollPaymentBatchStatus.Prepared;

    public decimal TotalAmount { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; }

    public PayrollPaymentChannel Channel { get; set; } = PayrollPaymentChannel.BankTransfer;
    [StringLength(50)] public string BankFormat { get; set; }

    public DateTime? ReleasedAt { get; set; }
    public Guid? ReleasedById { get; set; }
    public User ReleasedBy { get; set; }

    [StringLength(2000)] public string FailureReason { get; set; }

    public List<PayrollPaymentInstruction> Instructions { get; set; } = [];
    public List<PayrollPaymentBatchApproval> Approvals { get; set; } = [];

    public bool Approved { get; set; }
}

public class PayrollPaymentBatchApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }

    public Guid PayrollPaymentBatchId { get; set; }
    public PayrollPaymentBatch PayrollPaymentBatch { get; set; }

    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}

public enum PayrollPaymentBatchStatus
{
    Prepared = 0,
    PendingRelease = 1,
    Released = 2,
    PartiallySettled = 3,
    Settled = 4,
    Failed = 5,
    Cancelled = 6
}

public enum PayrollPaymentChannel
{
    BankTransfer = 0,
    MobileMoney = 1,
    Cash = 2,
    Cheque = 3,
    Mixed = 4
}