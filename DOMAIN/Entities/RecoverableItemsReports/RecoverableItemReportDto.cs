using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;

namespace DOMAIN.Entities.RecoverableItemsReports;

public class RecoverableItemReportDto : BaseDto
{
    public Guid ItemId { get; set; }
    public ItemDto Item { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; }
}