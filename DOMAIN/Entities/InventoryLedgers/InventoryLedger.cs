using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Warehouses;

namespace DOMAIN.Entities.InventoryLedgers;

public class InventoryLedger : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string TransactionType { get; set; } // e.g., "Adjustment", "Issue", "Receipt"

    [Required]
    [StringLength(100)]
    public string ReferenceId { get; set; } // AdjustmentNumber, OrderNumber, etc.

    // Poly-Reference
    public Guid? ItemId { get; set; }
    public Item Item { get; set; }

    public Guid? ShelfMaterialBatchId { get; set; }
    public ShelfMaterialBatch ShelfMaterialBatch { get; set; }

    public decimal ChangeAmount { get; set; }
    public decimal PostTransactionBalance { get; set; }

    [StringLength(1000)]
    public string Notes { get; set; }
}
