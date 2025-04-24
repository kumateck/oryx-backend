using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Requisitions;
using SHARED;

namespace DOMAIN.Entities.PurchaseOrders;

public class PurchaseOrder : BaseEntity, IRequireApproval
{
    [StringLength(100)] public string Code { get; set; }
    [StringLength(100)] public string ProFormaInvoiceNumber { get; set; }
    public Guid SourceRequisitionId { get; set; }
    public SourceRequisition SourceRequisition { get; set; }
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public List<PurchaseOrderItem> Items { get; set; } = [];
    public DateTime? DeliveryDate { get; set; }
    public DateTime? SentAt { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public bool IsRevised => RevisedPurchaseOrders.Count != 0;
    public int RevisionNumber { get; set; }
    public List<RevisedPurchaseOrder> RevisedPurchaseOrders { get; set; } = [];
    public DeliveryMode DeliveryMode { get; set; }
    public TermsOfPayment TermsOfPayment { get; set; }
    public Guid? DeliveryModeId { get; set; }
    public Guid? TermsOfPaymentId { get; set; }
    public decimal TotalFobValue { get; set; }
    public decimal TotalCifValue { get; set; }
    public decimal SeaFreight { get; set; }
    public decimal Insurance { get; set; }
    [StringLength(100)] public string AmountInFigures { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public List<PurchaseOrderApproval>  Approvals { get; set; } = [];
    public bool Approved { get; set; }
}

public class PurchaseOrderApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}


public class PurchaseOrderItem : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency Currency { get; set; }
}

public enum PurchaseOrderStatus
{
    Pending,
    Delivered,
    Attached,
    Completed
}

public enum PurchaseOrderAttachmentStatus
{
    None,
    Partial,
    Full
}

public class PurchaseOrderDto : WithAttachment
{
    public string Code { get; set; }
    public string ProFormaInvoiceNumber { get; set; }
    public SupplierDto Supplier { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public List<PurchaseOrderItemDto> Items { get; set; } = [];
    public PurchaseOrderStatus Status { get; set; }
    public bool IsRevised { get; set; }
    public PurchaseOrderAttachmentStatus AttachmentStatus { get; set; }
    public CollectionItemDto DeliveryMode { get; set; }
    public CollectionItemDto TermsOfPayment { get; set; }
    public decimal TotalFobValue { get; set; }
    public decimal TotalCifValue { get; set; }
    public decimal SeaFreight { get; set; }
    public string AmountInFigures { get; set; }
    public decimal Insurance { get; set; }
    public int RevisionNumber { get; set; }
    public List<PurchaseOrderRevisionDto> Revisions { get; set; } = [];
    public DateTime? EstimatedDeliveryDate { get; set; }
}

public class PurchaseOrderRevisionDto
{
    public int RevisionNumber { get; set; }
    public List<PurchaseOrderItemDto> Items { get; set; } = [];
}

public class PurchaseOrderItemDto
{
    public Guid Id { get; set; }
    public CollectionItemDto PurchaseOrder { get; set; }
    public CollectionItemDto Material { get; set; }
    public UnitOfMeasureDto Uom { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public CollectionItemDto Currency { get; set; }
    public List<ManufacturerDto> Manufacturers { get; set; } = [];
    public decimal Cost => Price * Quantity;
    public bool CanReassignSupplier { get; set; }
}