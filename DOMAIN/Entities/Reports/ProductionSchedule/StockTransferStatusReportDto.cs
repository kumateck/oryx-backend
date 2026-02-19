namespace DOMAIN.Entities.Reports.ProductionSchedule;

public class StockTransferStatusReportDto
{
    public int InProgressCount { get; set; }
    public int ApprovedCount { get; set; }
    public int IssuedCount { get; set; }
    public int RejectedCount { get; set; }
}