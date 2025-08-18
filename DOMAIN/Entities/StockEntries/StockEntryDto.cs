using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Memos;

namespace DOMAIN.Entities.StockEntries;

public class StockEntryDto : BaseDto
{
    public Guid ItemId { get; set; }
    public ItemDto Item { get; set; }
    public Guid MemoId { get; set; }
    public MemoDto Memo { get; set; }
    public decimal Quantity { get; set; }
    public ApprovalStatus Status { get; set; } 
}