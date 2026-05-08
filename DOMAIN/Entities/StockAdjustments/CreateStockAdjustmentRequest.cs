using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.StockAdjustments;

public class CreateStockAdjustmentRequest
{
    [Required]
    public string AdjustmentNumber { get; set; }

    [Required]
    public DateTime AdjustmentDate { get; set; }

    [Required]
    public StockAdjustmentTarget TargetType { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateStockAdjustmentLineRequest> Lines { get; set; } = [];
}

public class CreateStockAdjustmentLineRequest
{
    [Required]
    public Guid ModelId { get; set; } // ID of Item or ShelfMaterialBatch

    [Required]
    public decimal PhysicalCount { get; set; }

    [Required]
    public StockAdjustmentReasonCode ReasonCode { get; set; }

    public string Notes { get; set; }
}

public enum StockAdjustmentReasonCode
{
    PhysicalCount = 0,
    Damage = 1,
    Theft = 2,
    Expiry = 3,
    DataEntryError = 4,
    ReturnedGoods = 5,
    Other = 6,
}
