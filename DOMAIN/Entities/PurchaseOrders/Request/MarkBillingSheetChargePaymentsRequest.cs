using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Payments;

namespace DOMAIN.Entities.PurchaseOrders.Request;

public class MarkBillingSheetChargePaymentsRequest
{
    public DateTime PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    [MinLength(1)]
    public List<MarkBillingSheetChargePayment> Charges { get; set; } = [];
}

public class MarkBillingSheetChargePayment
{
    public Guid BillingSheetChargeId { get; set; }
    [Required, StringLength(255)] public string Reference { get; set; } = string.Empty;
    [StringLength(2000)] public string Notes { get; set; }
}

public class MarkBillingSheetChargePaymentsResponse
{
    public List<Guid> PaidChargeIds { get; set; } = [];
    public List<Guid> PendingChargeIds { get; set; } = [];
}
