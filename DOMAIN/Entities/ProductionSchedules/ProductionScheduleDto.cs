using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products;
using SHARED;

namespace DOMAIN.Entities.ProductionSchedules;

public class ProductionScheduleDto : BaseDto
{
    public string Code { get; set; }
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public ProductionStatus Status { get; set; } 
    public string Remarks { get; set; } 
    public List<ProductionScheduleProductDto> Products { get; set; } = [];
}

public class ProductionScheduleItemDto : BaseDto
{
    public CollectionItemDto Material { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal Quantity { get; set; }
}

public class ProductionScheduleProcurementDto 
{
    public MaterialDto Material { get; set; }
    public UnitOfMeasureDto BaseUoM { get; set; }
    public decimal BaseQuantity { get; set; }
    public decimal QuantityNeeded { get; set; }
    public decimal QuantityOnHand { get; set; }
}

public class ProductionScheduleProductDto
{
    public ProductListDto Product { get; set; }
    public decimal Quantity { get; set; }
}
