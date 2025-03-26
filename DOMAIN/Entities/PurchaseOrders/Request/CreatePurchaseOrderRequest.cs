namespace DOMAIN.Entities.PurchaseOrders.Request;

public class CreatePurchaseOrderRequest
{
    public string Code { get; set; }
    public Guid SupplierId { get; set; }
    public Guid SourceRequisitionId { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public List<CreatePurchaseOrderItemRequest> Items { get; set; } = [];
}

public class CreatePurchaseOrderItemRequest 
{
    public Guid MaterialId { get; set; }
    public Guid UomId { get; set; }
    public Guid? CurrencyId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
}


public class CreatePurchaseOrderRevision
{
    public RevisedPurchaseOrderType Type { get; set; }
    public Guid? PurchaseOrderItemId { get; set; }
    public Guid? MaterialId { get; set; }
    public Guid? UoMId { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? Price { get; set; }
    public Guid? CurrencyId { get; set; }
}