using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.ItemTransactionLogs;

public class ItemTransactionLog : BaseEntity
{
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string TransactionType { get; set; }
    public string ItemCode { get; set; }
    public decimal Credit { get; set; }
    public decimal Debit { get; set; }
    public decimal? ShadowHold { get; set; }
    public decimal TotalBalance { get; init; }
}

public class ItemTransactionLogDto : BaseDto
{
    public DateTime Date { get; set; }
    public string TransactionType { get; set; }
    public string ItemCode { get; set; }
    public decimal Credit { get; set; }
    public decimal Debit { get; set; }
    public decimal TotalBalance => Credit - Debit;
    public decimal ShadowHold { get; set; }

}