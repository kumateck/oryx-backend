namespace DOMAIN.Entities.PurchaseOrders.Request;

public class CreatePurchaseOrderRequest
{
    public Guid SupplierId { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public List<CreatePurchaseOrderItemRequest> Items { get; set; } = [];
}

public class CreatePurchaseOrderItemRequest 
{
    public Guid MaterialId { get; set; }
    public Guid UomId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
