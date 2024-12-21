using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.WorkOrders;

namespace DOMAIN.Entities.ProductionSchedules;

public class ProductionSchedule : BaseEntity
{
    [StringLength(100)] public string Code { get; set; }
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public Guid? ProductId { get; set; }
    public Product Product { get; set; }
    public ProductionStatus Status { get; set; } 
    public decimal Quantity { get; set; } // Quantity of the product to be produced
    [StringLength(1000)] public string Remarks { get; set; } // Optional remarks for additional notes
    public List<ProductionScheduleItem> Items { get; set; } = [];

}

public class ProductionScheduleItem : BaseEntity
{
    public Guid ProductionScheduleId { get; set; }
    public ProductionSchedule ProductionSchedule { get; set; }
    public Guid? UomId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public decimal Quantity { get; set; }
}
