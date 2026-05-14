namespace DOMAIN.Entities.ProductionSchedules;

public class UpdateProductionScheduleRequest
{
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public string Remarks { get; set; }
}