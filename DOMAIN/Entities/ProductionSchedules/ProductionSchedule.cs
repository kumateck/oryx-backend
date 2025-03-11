using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.ProductionSchedules;

public class ProductionSchedule : BaseEntity
{
    [StringLength(100)] public string Code { get; set; }
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public ProductionStatus Status { get; set; } 
    [StringLength(1000)] public string Remarks { get; set; }
    public List<ProductionScheduleProduct> Products { get; set; } = [];
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

public class ProductionScheduleProduct
{
    public Guid Id { get; set; }
    public Guid ProductionScheduleId { get; set; }
    public ProductionSchedule ProductionSchedule { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; }   
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public decimal Quantity { get; set; }
}