using DOMAIN.Entities.Base;
using DOMAIN.Entities.Memos;

namespace DOMAIN.Entities.ItemInventoryTransactions;

public class ItemInventoryTransaction : BaseEntity
{
    public DateTime Date { get; set; }
    public Guid MemoId { get; set; }
    public Memo Memo { get; set; }
    public string BatchNumber { get; set; }
    public int QuantityReceived { get; set; }
    public int QuantityIssued { get; set; }
    public int BalanceQuantity => QuantityReceived - QuantityIssued;
}