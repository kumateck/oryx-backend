using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.StockAdjustments;

public class CreateStockAdjustmentRequest
{
    [Required]
    public string AdjustmentNumber { get; set; }

    [Required]
    public Guid WarehouseId { get; set; }

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
    public Guid ProductId { get; set; } // ID of Item or ShelfMaterialBatch

    [Required]
    public decimal PhysicalCount { get; set; }

    [Required]
    public string ReasonCode { get; set; }

    public string Notes { get; set; }
}
