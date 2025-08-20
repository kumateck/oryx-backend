using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Items;

namespace DOMAIN.Entities.DamagedStocks;

public class DamagedStockDto : WithAttachment
{
    public Guid ItemId { get; set; }
    public ItemDto Item { get; set; }
    public DamageStatus DamageStatus {get; set;}
    public string Remarks {get; set;}
}