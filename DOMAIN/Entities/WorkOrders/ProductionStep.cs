using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.WorkOrders;

public class ProductionStep : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public WorkOrder WorkOrder { get; set; }
    [StringLength(200)] public string Description { get; set; }
    public Guid? ResourceId { get; set; } // e.g., Machine or Labor resource
    public Resource Resource { get; set; }
    public TimeSpan Duration { get; set; } // Expected duration for the step
    public DateTime? StartDate { get; set; } // Actual start date
    public DateTime? EndDate { get; set; } // Actual end date
    public ProductionStatus Status { get; set; } // e.g., Not Started, In Progress, Completed
    public int Sequence { get; set; } // To track order of steps within a work order
}