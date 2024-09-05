using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.WorkOrders;

namespace DOMAIN.Entities.ProductionSchedules;

public class MasterProductionSchedule : BaseEntity
{
    public Guid ProductId { get; set; } // Links to the product being planned
    public Product Product { get; set; }

    public DateTime PlannedStartDate { get; set; } // When production is scheduled to start
    public DateTime PlannedEndDate { get; set; } // When production is scheduled to finish
    public int PlannedQuantity { get; set; } // How much of the product is planned to be produced

    public ProductionStatus Status { get; set; } // e.g., Planned, In Progress, Completed

    public List<WorkOrder> WorkOrders { get; set; } // Links to work orders that fulfill this MPS
}
