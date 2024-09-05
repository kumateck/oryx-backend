using DOMAIN.Entities.Base;
using DOMAIN.Entities.WorkOrders;

namespace DOMAIN.Entities.ProductionSchedules;

public class CreateProductionScheduleRequest
{
    public Guid WorkOrderId { get; set; }
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public Guid ResourceId { get; set; }
    public ProductionStatus Status { get; set; } 
    public string Remarks { get; set; } // Optional remarks for additional notes
}