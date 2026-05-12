using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.PurchaseOrders.Request;

public class CreatePurchaseOrderRequest
{
    public string Code { get; set; }
    public string ProFormaInvoiceNumber { get; set; }
    public Guid SupplierId { get; set; }
    public Guid SourceRequisitionId { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public List<CreatePurchaseOrderItemRequest> Items { get; set; } = [];
    public Guid? DeliveryModeId { get; set; }
    public Guid? TermsOfPaymentId { get; set; }
    public decimal TotalFobValue { get; set; }
    public decimal TotalCifValue { get; set; }
    public decimal SeaFreight { get; set; }
    public decimal Insurance { get; set; }
    public string AmountInFigures { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public Guid? DepartmentId { get; set; }
}

public class UpdatePurchaseOrderFirstStep
{
    public string ProFormaInvoiceNumber { get; set; }
    public Guid? DeliveryModeId { get; set; }
    public Guid? TermsOfPaymentId { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
}

public class UpdatePurchaseOrderRequest
{
    public decimal TotalFobValue { get; set; }
    public decimal TotalCifValue { get; set; }
    public decimal SeaFreight { get; set; }
    public decimal Insurance { get; set; }
    public string AmountInFigures { get; set; }
}

public class CreatePurchaseOrderItemRequest
{
    public Guid MaterialId { get; set; }
    public Guid UomId { get; set; }
    public Guid? CurrencyId { get; set; }
    public decimal Quantity { get; set; }
    [Range(0.0001, double.MaxValue, ErrorMessage = "Price must be greater than 0.0001")]
    public decimal Price { get; set; }
    public string PriceUoM { get; set; }
}


public record CreatePurchaseOrderRevision
{
    public RevisedPurchaseOrderType Type { get; set; }
    public Guid? PurchaseOrderItemId { get; set; }
    public Guid? MaterialId { get; set; }
    public Guid? UoMId { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? Price { get; set; }
    public string PriceUoM { get; set; }
    public Guid? CurrencyId { get; set; }
}

public record EnrichedRevision : CreatePurchaseOrderRevision
{
    public Guid? UoMBeforeId { get; set; }
    public decimal? QuantityBefore { get; set; }
    public decimal? PriceBefore { get; set; }
    public string PriceUoMBefore { get; set; }
    public Guid? CurrencyBeforeId { get; set; }
    public Guid? MaterialBeforeId { get; set; }
    public int RevisionNumber { get; set; }
}
