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
}
