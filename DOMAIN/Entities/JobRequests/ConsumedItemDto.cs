using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;

namespace DOMAIN.Entities.JobRequests;

public class ConsumedItemDto : BaseDto
{
    public Guid? JobExecutionId { get; set; }
    public Guid? JobOrderExecutionId { get; set; }
    public Guid? QuotationItemId { get; set; }
    public ItemDto Item { get; set; }
    public decimal QuantityConsumed { get; set; }
    public UnitOfMeasureDto UnitOfMeasure { get; set; }
    public string Notes { get; set; }
    public ItemSource Source { get; set; }
}

