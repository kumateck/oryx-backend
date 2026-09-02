using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;

namespace DOMAIN.Entities.Payments;

public class RecordPaymentRequest
{
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }
    public Guid CurrencyId { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    [Required, StringLength(255)] public string Reference { get; set; }
    [StringLength(2000)] public string Notes { get; set; }
    public PayableType PayableType { get; set; }
    public Guid PayableId { get; set; }
}

public class ReviewPaymentRequest
{
    public ApprovalStatus Status { get; set; }
    [StringLength(1000)] public string Comments { get; set; }
}
