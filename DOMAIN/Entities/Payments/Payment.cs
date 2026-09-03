using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Payments;

public class Payment : BaseEntity, IRequireApproval
{
    public decimal Amount { get; set; }
    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    [Required, StringLength(255)] public string Reference { get; set; }
    [StringLength(2000)] public string Notes { get; set; }
    public Guid RecordedById { get; set; }
    public User RecordedBy { get; set; }
    public PayableType PayableType { get; set; }
    public Guid PayableId { get; set; }
    public Guid? BillingSheetChargeId { get; set; }
    public BillingSheetCharge BillingSheetCharge { get; set; }
    public bool Approved { get; set; }
    public PaymentStatus Status { get; set; }
    public List<PaymentApproval> Approvals { get; set; } = [];
}

public class PaymentApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}

public enum PaymentMethod
{
    BankTransfer = 0,
    Cheque = 1,
    Cash = 2,
    CreditCard = 3,
    Other = 4,
}

public enum PayableType
{
    BillingSheet = 0,
    ShipmentInvoice = 1,
    PurchaseOrderInvoice = 2,
    CustomerInvoice = 3,
}

public enum PaymentStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}
