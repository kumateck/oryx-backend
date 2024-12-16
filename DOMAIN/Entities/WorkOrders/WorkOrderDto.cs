using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionSchedules;
using SHARED;

namespace DOMAIN.Entities.WorkOrders;

public class WorkOrderDto : BaseDto
{
    public string Code { get; set; }
    public CollectionItemDto Product { get; set; } // Navigation to the Product entity
    public MasterProductionScheduleDto MasterProductionSchedule { get; set; } // Navigation to MPS
    public int Quantity { get; set; } // Quantity of the product to be produced
    public DateTime StartDate { get; set; } // Scheduled start date
    public DateTime EndDate { get; set; } // Scheduled end date
    public ProductionStatus Status { get; set; } // Status of the work order (e.g., Planned, In Progress, Completed, Canceled)
    public string BatchNumber { get; set; } // Unique identifier for batch tracking
    public List<ProductionStepDto> Steps { get; set; } = []; // Steps involved in the production
}