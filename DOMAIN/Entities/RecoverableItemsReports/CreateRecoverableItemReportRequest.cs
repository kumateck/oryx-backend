using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.RecoverableItemsReports;

public class CreateRecoverableItemReportRequest
{
    [Required] public Guid ItemId { get; set; }
    [Required] public int Quantity { get; set; }
    public string Reason { get; set; }
    /// <summary>The requisition line being returned against; required, so a return never exceeds what was issued.</summary>
    public Guid? ItemStockRequisitionItemId { get; set; }
}