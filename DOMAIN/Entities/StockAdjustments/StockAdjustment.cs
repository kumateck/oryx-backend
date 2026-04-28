using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Warehouses;

namespace DOMAIN.Entities.StockAdjustments;

public class StockAdjustment : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string AdjustmentNumber { get; set; }

    public DateTime AdjustmentDate { get; set; }

    public StockAdjustmentTarget TargetType { get; set; }

    public List<StockAdjustmentLine> Lines { get; set; } = [];
}

public enum StockAdjustmentTarget
{
    Item = 0,
    Material = 1,
}
