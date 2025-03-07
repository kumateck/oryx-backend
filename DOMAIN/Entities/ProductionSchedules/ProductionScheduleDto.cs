using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
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
    public MaterialRequisitionStatus Status { get; set; }
    public List<BatchLocation> Batches { get; set; } = [];
}

public class ProductionScheduleProcurementPackageDto 
{
    public MaterialDto Material { get; set; }
    public MaterialDto DirectLinkMaterial { get; set; }
    public UnitOfMeasureDto BaseUoM { get; set; }
    public decimal BaseQuantity { get; set; }
    public decimal QuantityNeeded { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal UnitCapacity { get; set; }
    public MaterialRequisitionStatus Status { get; set; }
    public decimal PackingExcessMargin { get; set; }
}

public class ProductionScheduleProductDto
{
    public ProductListDto Product { get; set; }
    public decimal Quantity { get; set; }
}

public enum MaterialRequisitionStatus
{
    None = 0,
    StockTransfer = 1,
    PurchaseRequisition = 2,
    Local = 3,
    Foreign= 4,
    StockRequisition = 5,
    Issued = 6,
    InHouse = 7
}