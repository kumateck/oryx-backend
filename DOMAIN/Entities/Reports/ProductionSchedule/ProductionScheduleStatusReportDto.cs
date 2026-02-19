namespace DOMAIN.Entities.Reports.ProductionSchedule;

public class ProductionScheduleStatusReportDto
{
    public int NewScheduleCount { get; set; }
    public int InProgressScheduleCount { get; set; }
    public int CompletedScheduleCount { get; set; }
    public int DelayedScheduleCount { get; set; }
    public int CancelledScheduleCount { get; set; }
}