using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Manufacturers;
using SHARED;

namespace DOMAIN.Entities.PurchaseOrders;

public class RevisedPurchaseOrder
{
    public Guid Id { get; set; }
    public RevisedPurchaseOrderType Type { get; set; }
    public Guid? PurchaseOrderItemId { get; set; }
    public PurchaseOrderItem PurchaseOrderItem { get; set; }
    public Guid? MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid? UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? Price { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency Currency { get; set; }
}

public class RevisedPurchaseOrderItem : BaseEntity
{
    public Guid RevisedPurchaseOrderId { get; set; }
    public RevisedPurchaseOrder RevisedPurchaseOrder { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency Currency { get; set; }
}

public class RevisedPurchaseOrderDto : BaseDto
{
    public string Code { get; set; }
    public CollectionItemDto Supplier { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public List<PurchaseOrderItemDto> Items { get; set; } = [];
    public PurchaseOrderStatus Status { get; set; }
}

public class RevisedPurchaseOrderItemDto
{
    public CollectionItemDto RevisedPurchaseOrder { get; set; }
    public CollectionItemDto Material { get; set; }
    public CollectionItemDto Currency { get; set; }
    public UnitOfMeasureDto Uom { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public List<ManufacturerDto> Manufacturers { get; set; } = [];
    public decimal Cost => Price * Quantity;
}

public enum RevisedPurchaseOrderType
{
    ReassignSuppler,
    ChangeSource,
    AddItem,
    UpdateItem,
    RemoveItem
}