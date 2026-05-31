namespace DOMAIN.Entities.StockAdjustments;

public class StockAdjustmentSummaryDto
{
    public Guid Id { get; set; }
    public string AdjustmentNumber { get; set; }
    public DateTime AdjustmentDate { get; set; }
    public int LinesProcessed { get; set; }
    public decimal TotalVariance { get; set; }
    public bool Approved { get; set; }
    public StockAdjustmentTarget TargetType { get; set; }
    public List<StockAdjustmentLineSummaryDto> Lines { get; set; } = [];
}

public class StockAdjustmentLineSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string CodeOrBatch { get; set; }
    public decimal PhysicalCount { get; set; }
    public decimal SystemQuantitySnapshot { get; set; }
    public decimal Variance { get; set; }
    public string UomSymbol { get; set; }
}
