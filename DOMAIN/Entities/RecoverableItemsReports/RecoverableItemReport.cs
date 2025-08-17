using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;

namespace DOMAIN.Entities.RecoverableItemsReports;

public class RecoverableItemReport : BaseEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; }
}

public class RecoverableItemBatchReport : BaseEntity
{
    public Guid RecoverableItemReportId { get; set; }
    public RecoverableItemReport RecoverableItemReport { get; set; }
    
}