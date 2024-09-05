using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.WorkOrders;

namespace DOMAIN.Entities.ProductionSchedules;

public class ProductionSchedule : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public WorkOrder WorkOrder { get; set; }
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public Guid? ResourceId { get; set; }
    public Resource Resource { get; set; }
    public ProductionStatus Status { get; set; } 
    [StringLength(1000)] public string Remarks { get; set; } // Optional remarks for additional notes
}
