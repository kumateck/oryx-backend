using DOMAIN.Entities.Base;
using DOMAIN.Entities.WorkOrders;

namespace DOMAIN.Entities.ProductionSchedules;

public class ProductionScheduleDto
{
    public WorkOrderDto WorkOrder { get; set; }
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public ResourceDto Resource { get; set; }
    public ProductionStatus Status { get; set; } 
    public string Remarks { get; set; } // Optional remarks for additional notes
}