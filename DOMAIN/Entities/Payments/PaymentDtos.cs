using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Payments;

public class PaymentDto : BaseDto
{
    public decimal Amount { get; set; }
    public CurrencyDto Currency { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    public string Reference { get; set; }
    public string Notes { get; set; }
    public UserDto RecordedBy { get; set; }
    public PayableType PayableType { get; set; }
    public Guid PayableId { get; set; }
    public bool Approved { get; set; }
    public PaymentStatus Status { get; set; }
    public List<PaymentApprovalDto> Approvals { get; set; } = [];
}

public class PaymentApprovalDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public bool Required { get; set; }
    public ApprovalStatus Status { get; set; }
    public DateTime? ApprovalTime { get; set; }
    public string Comments { get; set; }
}

public class PayableBalanceDto
{
    public Guid CurrencyId { get; set; }
    public string CurrencyName { get; set; }
    public string CurrencySymbol { get; set; }
    public decimal DocumentTotal { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal OutstandingBalance { get; set; }
}
