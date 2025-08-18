using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Items;

namespace DOMAIN.Entities.RecoverableItemsReports;

public class CreateRecoverableItemReportRequest
{
    [Required] public Guid ItemId { get; set; }
    [Required] public int Quantity { get; set; }
    public string Reason { get; set; }
}