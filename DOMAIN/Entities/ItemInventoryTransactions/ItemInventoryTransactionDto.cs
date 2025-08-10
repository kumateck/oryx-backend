using DOMAIN.Entities.Base;
using DOMAIN.Entities.Memos;

namespace DOMAIN.Entities.ItemInventoryTransactions;

public class ItemInventoryTransactionDto : BaseDto
{
    public DateTime Date { get; set; }
    public Guid MemoId { get; set; }
    public MemoDto Memo { get; set; }
    public int QuantityReceived { get; set; }
    public int QuantityIssued { get; set; }
    public int BalanceQuantity { get; set; }
}