using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.ProductionSchedules;

public class CreateProductionScheduleRequest
{
    [StringLength(100)] public string Code { get; set; }
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public Guid? ProductId { get; set; }
    public ProductionStatus Status { get; set; } 
    public int Quantity { get; set; } // Quantity of the product to be produced
    [StringLength(1000)] public string Remarks { get; set; } // Optional remarks for additional notes
    public List<CreateProductionScheduleItemRequest> Items { get; set; } = [];
}

public class CreateProductionScheduleItemRequest
{
    public Guid MaterialId { get; set; }
    public Guid? UomId { get; set; }
    public int Quantity { get; set; }
}