using System.ComponentModel.DataAnnotations;


namespace DOMAIN.Entities.ItemInventoryTransactions;

public class CreateItemInventoryTransactionRequest
{
    [Required] public DateTime Date { get; set; }
    [Required] public Guid MemoId { get; set; }
    [Required, Range(1, int.MaxValue)] public int QuantityReceived { get; set; }
    [Required, Range(1, int.MaxValue)] public int QuantityIssued { get; set; }
}