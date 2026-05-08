namespace DOMAIN.Entities.StockAdjustments;

public class StockAdjustmentSummaryDto
{
    public Guid Id { get; set; }
    public string AdjustmentNumber { get; set; }
    public int LinesProcessed { get; set; }
    public decimal TotalVariance { get; set; }
}
