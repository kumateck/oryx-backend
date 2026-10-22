using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;

namespace DOMAIN.Entities.RecoverableItemsReports;

public class RecoverableItemReportDto : BaseDto
{
    public ItemDto Item { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; }
    public Store Store { get; set; }
    public Guid? ItemStockRequisitionId { get; set; }
    public Guid? ItemStockRequisitionItemId { get; set; }
    public string RequisitionNumber { get; set; }
}