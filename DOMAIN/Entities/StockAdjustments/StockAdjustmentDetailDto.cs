using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.StockAdjustments;

public class StockAdjustmentDetailDto
{
    public Guid Id { get; set; }
    public string AdjustmentNumber { get; set; }
    public DateTime AdjustmentDate { get; set; }
    public bool Approved { get; set; }
    public StockAdjustmentTarget TargetType { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<StockAdjustmentLineDetailDto> Lines { get; set; } = [];
}

public class StockAdjustmentLineDetailDto
{
    public Guid Id { get; set; }
    public Guid? ItemId { get; set; }
    public string ItemName { get; set; }
    public string ItemCode { get; set; }

    public Guid? ShelfMaterialBatchId { get; set; }
    public string BatchNumber { get; set; }
    public string MaterialName { get; set; }
    public string ShelfName { get; set; }

    public Guid? FinishedGoodsTransferNoteId { get; set; }
    public string TransferNoteNumber { get; set; }
    public string ProductName { get; set; }

    public decimal PhysicalCount { get; set; }
    public decimal SystemQuantitySnapshot { get; set; }
    public decimal Variance { get; set; }
    public UnitOfMeasureDto Uom { get; set; }
    public string ReasonCode { get; set; }
    public string Notes { get; set; }
}
