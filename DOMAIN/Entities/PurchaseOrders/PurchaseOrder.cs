using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Requisitions;
using SHARED;

namespace DOMAIN.Entities.PurchaseOrders;

public class PurchaseOrder : BaseEntity
{
    [StringLength(100)] public string Code { get; set; }
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
    public List<RevisedPurchaseOrder> RevisedPurchaseOrders { get; set; } = [];
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
    public SupplierDto Supplier { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public List<PurchaseOrderItemDto> Items { get; set; } = [];
    public PurchaseOrderStatus Status { get; set; }
    public List<RevisedPurchaseOrderDto> RevisedPurchaseOrders { get; set; } = [];
    public PurchaseOrderAttachmentStatus AttachmentStatus { get; set; }
}

public class PurchaseOrderItemDto
{
    public CollectionItemDto PurchaseOrder { get; set; }
    public CollectionItemDto Material { get; set; }
    public UnitOfMeasureDto Uom { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public CollectionItemDto Currency { get; set; }
    public List<ManufacturerDto> Manufacturers { get; set; } = [];
    public decimal Cost => Price * Quantity;
}