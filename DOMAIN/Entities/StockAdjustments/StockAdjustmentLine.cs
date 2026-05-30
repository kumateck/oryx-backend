using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;

namespace DOMAIN.Entities.StockAdjustments;

public class StockAdjustmentLine : BaseEntity
{
    public Guid StockAdjustmentId { get; set; }
    public StockAdjustment StockAdjustment { get; set; }

    // Reference to the item being adjusted (if TargetType is Item)
    public Guid? ItemId { get; set; }
    public Item Item { get; set; }

    // Reference to the shelf material batch being adjusted (if TargetType is Material)
    public Guid? ShelfMaterialBatchId { get; set; }
    public ShelfMaterialBatch ShelfMaterialBatch { get; set; }

    // Reference to the finished goods transfer note being adjusted (if TargetType is Product)
    public Guid? FinishedGoodsTransferNoteId { get; set; }
    public FinishedGoodsTransferNote FinishedGoodsTransferNote { get; set; }

    public decimal PhysicalCount { get; set; }
    public decimal SystemQuantitySnapshot { get; set; }
    public decimal Variance { get; set; }
    public Guid? UomId { get; set; }
    public UnitOfMeasure UoM { get; set; }

    [Required]
    [StringLength(1000)]
    public string ReasonCode { get; set; }

    [StringLength(10000)]
    public string Notes { get; set; }
}
