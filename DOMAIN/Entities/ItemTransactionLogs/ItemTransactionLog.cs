using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.ItemTransactionLogs;

public class ItemTransactionLog : BaseEntity
{
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public TransactionType TransactionType { get; set; }
    public string ItemCode { get; set; }
    public decimal Credit { get; set; }
    public decimal Debit { get; set; }
    public decimal? ShadowHold { get; set; }
    public decimal TotalBalance { get; init; }
}

public enum TransactionType
{
    Damaged,
    Missing,
    Returned,
    Issued,
    Purchased
    
}

public class ItemTransactionLogDto : BaseDto
{
    public DateTime Date { get; set; }
    public string TransactionType { get; set; }
    public string ItemCode { get; set; }
    public decimal Credit { get; set; }
    public decimal Debit { get; set; }
    public decimal TotalBalance { get; set; }
    public decimal ShadowHold { get; set; }

}