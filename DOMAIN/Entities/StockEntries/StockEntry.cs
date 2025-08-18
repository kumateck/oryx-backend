using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Memos;

namespace DOMAIN.Entities.StockEntries;

public class StockEntry : BaseEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; }
    public Guid MemoId { get; set; }
    public Memo Memo { get; set; }
    public decimal Quantity { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
}
