using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;

namespace DOMAIN.Entities.RecoverableItemsReports;

public class RecoverableItemReport : BaseEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; }

    /// <summary>The requisition line the returned stock was issued against. Older returns have none.</summary>
    public Guid? ItemStockRequisitionItemId { get; set; }
    public ItemStockRequisitionItem ItemStockRequisitionItem { get; set; }
}

public class RecoverableItemBatchReport : BaseEntity
{
    public Guid ItemId { get; set; }
    public ItemDto Item { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; }

}