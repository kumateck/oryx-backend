using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.ProductionSchedules;

public class CreateProductionScheduleRequest
{
    [StringLength(100)] public string Code { get; set; }
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public List<CreateProductionScheduleProduct> Products { get; set; } = [];
    public ProductionStatus Status { get; set; } 
    [StringLength(1000)] public string Remarks { get; set; } // Optional remarks for additional notes
}

public class CreateProductionScheduleItemRequest
{
    public Guid MaterialId { get; set; }
    public Guid? UomId { get; set; }
    public decimal Quantity { get; set; }
}

public class CreateProductionScheduleProduct
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
}