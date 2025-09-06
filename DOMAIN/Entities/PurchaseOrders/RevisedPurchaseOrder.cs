using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Materials;

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
    public Guid? UoMBeforeId { get; set; }
    public UnitOfMeasure UomBefore { get; set; }
    public decimal? QuantityBefore { get; set; }
    public decimal? PriceBefore { get; set; }
    public Guid? CurrencyBeforeId { get; set; }
    public Currency CurrencyBefore { get; set; }
    public Guid? MaterialBeforeId { get; set; }
    public Material MaterialBefore { get; set; }
    public int RevisionNumber { get; set; }
    public DateTime? RevisionDate { get; set; }
}

public class RevisedPurchaseOrderDto
{
    public Guid Id { get; set; }
    public RevisedPurchaseOrderType Type { get; set; }
    public MaterialDto Material { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? Price { get; set; }
    public CurrencyDto Currency { get; set; }
    public UnitOfMeasureDto UomBefore { get; set; }
    public decimal? QuantityBefore { get; set; }
    public decimal? PriceBefore { get; set; }
    public CurrencyDto CurrencyBefore { get; set; }
    public MaterialDto MaterialBefore { get; set; }
    public int RevisionNumber { get; set; }
    public DateTime? RevisionDate { get; set; }
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

public enum RevisedPurchaseOrderType
{
    ReassignSuppler,
    ChangeSource,
    AddItem,
    UpdateItem,
    RemoveItem
}

public class PurchaseOrderItemSnapshot
{
    public Guid Id { get; set; } 
    public Guid? MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid? UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency Currency { get; set; }
    public Guid? UoMBeforeId { get; set; }
    public UnitOfMeasure UomBefore { get; set; }
    public decimal? QuantityBefore { get; set; }
    public decimal? PriceBefore { get; set; }
    public Guid? CurrencyBeforeId { get; set; }
    public Currency CurrencyBefore { get; set; }
}
