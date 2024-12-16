using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.WorkOrders;

public class CreateWorkOrderRequest
{
    [StringLength(100)] public string Code { get; set; }
    public Guid ProductId { get; set; }
    public Guid? MasterProductionScheduleId { get; set; } // Link to the Master Production Schedule
    
    public int Quantity { get; set; } // Quantity of the product to be produced

    public DateTime StartDate { get; set; } // Scheduled start date
    public DateTime EndDate { get; set; } // Scheduled end date
        
    public ProductionStatus Status { get; set; }
        
    public string BatchNumber { get; set; } // Unique identifier for batch tracking
        
    public List<CreateProductionStepRequest> Steps { get; set; } = []; // Steps involved in the production
}