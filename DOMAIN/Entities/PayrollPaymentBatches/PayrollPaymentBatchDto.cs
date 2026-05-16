using DOMAIN.Entities.Base;
using DOMAIN.Entities.PayrollPaymentInstructions;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.PayrollPaymentBatches;

public class PayrollPaymentBatchDto : BaseDto
{
    public string Code { get; set; }
    public CollectionItemDto PayrollRun { get; set; }
    public PayrollPaymentBatchStatus BatchStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public CollectionItemDto Currency { get; set; }
    public PayrollPaymentChannel Channel { get; set; }
    public string BankFormat { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public UserDto ReleasedBy { get; set; }
    public string FailureReason { get; set; }
    public bool Approved { get; set; }
    public List<PayrollPaymentInstructionDto> Instructions { get; set; } = [];
}

public class ReleasePaymentBatchRequest
{
    public string Comment { get; set; }
}
