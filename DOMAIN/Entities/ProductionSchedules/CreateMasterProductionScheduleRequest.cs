using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.ProductionSchedules;

public class CreateMasterProductionScheduleRequest
{
    public Guid ProductId { get; set; } // Links to the product being planned

    public DateTime PlannedStartDate { get; set; } // When production is scheduled to start
    public DateTime PlannedEndDate { get; set; } // When production is scheduled to finish
    public int PlannedQuantity { get; set; } // How much of the product is planned to be produced

    public ProductionStatus Status { get; set; } // e.g., Planned, In Progress, Completed
}