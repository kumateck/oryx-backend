using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Suppliers;
using SHARED;

namespace DOMAIN.Entities.PurchaseOrders;

public class PurchaseOrder : BaseEntity
{
    [StringLength(100)] public string Code { get; set; }
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime ExpectedDeliveryDate { get; set; }
    public List<PurchaseOrderItem> Items { get; set; } = [];
    public DateTime? DeliveryDate { get; set; }
    public DateTime? SentAt { get; set; }
}

public class PurchaseOrderItem : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid UomId { get; set; }
    public UnitOfMeasure Uom { get; set; }
    public int Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency Currency { get; set; }
}

public class PurchaseOrderDto : BaseDto
{
    public CollectionItemDto Supplier { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime ExpectedDeliveryDate { get; set; }
    public List<PurchaseOrderItemDto> Items { get; set; } = [];
}

public class PurchaseOrderItemDto
{
    public CollectionItemDto PurchaseOrder { get; set; }
    public CollectionItemDto Material { get; set; }
    public CollectionItemDto Uom { get; set; }
    public int Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
    public CollectionItemDto Currency { get; set; }
    public decimal Cost => PricePerUnit * Quantity;
}