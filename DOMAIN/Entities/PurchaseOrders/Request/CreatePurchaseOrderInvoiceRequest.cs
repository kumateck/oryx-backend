namespace DOMAIN.Entities.PurchaseOrders.Request;

public class CreatePurchaseOrderInvoiceRequest
{
    public string Code { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public List<CreateBatchItemRequest> BatchItems { get; set; } = [];
    public List<CreatePurchaseOrderChargeRequest> Charges { get; set; } = [];
}

public class CreateBatchItemRequest 
{ 
    public string BatchNumber { get; set; }
    public Guid ManufacturerId { get; set; }
    public int Quantity { get; set; }
}

public class CreatePurchaseOrderChargeRequest 
{
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public Guid? CurrencyId { get; set; }
}