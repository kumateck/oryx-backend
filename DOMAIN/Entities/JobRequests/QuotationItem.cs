using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;

namespace DOMAIN.Entities.JobRequests;

/// <summary>
/// Represents materials/items required in a service quotation
/// </summary>
public class QuotationItem : BaseEntity
{
    public Guid ServiceQuotationId { get; set; }
    public ServiceQuotation ServiceQuotation { get; set; }
    public Guid? ItemId { get; set; }
    public Item Item { get; set; }
    public decimal Quantity { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
    // Track if this was negotiated
    public decimal? NegotiatedUnitPrice { get; set; }
    public decimal? NegotiatedTotalPrice => NegotiatedUnitPrice.HasValue
        ? Quantity * NegotiatedUnitPrice.Value
        : TotalPrice;
}

