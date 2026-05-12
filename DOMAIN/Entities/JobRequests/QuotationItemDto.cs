using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;

namespace DOMAIN.Entities.JobRequests;

public class QuotationItemDto : BaseDto
{
    public Guid ServiceQuotationId { get; set; }
    public ItemDto Item { get; set; }
    public decimal Quantity { get; set; }
    public UnitOfMeasureDto UnitOfMeasure { get; set; }
    public decimal UnitPrice { get; set; }
    public string PriceUoM { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal? NegotiatedUnitPrice { get; set; }
    public decimal? NegotiatedTotalPrice { get; set; }
}

