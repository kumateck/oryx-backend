using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.WorkOrders;

public class WorkOrder : BaseEntity
{
    [StringLength(100)] public string Code { get; set; }
    public Guid? ProductionScheduleId { get; set; }
    public ProductionSchedule ProductionSchedule { get; set; }
    public int Quantity { get; set; } // Quantity of the product to be produced
    public DateTime StartDate { get; set; } // Scheduled start date
    public DateTime EndDate { get; set; } // Scheduled end date
    public ProductionStatus Status { get; set; } // Status of the work order (e.g., Planned, In Progress, Completed, Canceled)
    [StringLength(100)] public string BatchNumber { get; set; } // Unique identifier for batch tracking
        
    public List<ProductionStep> Steps { get; set; } = new(); // Steps involved in the production
}