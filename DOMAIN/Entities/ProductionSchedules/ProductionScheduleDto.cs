using DOMAIN.Entities.Base;
using DOMAIN.Entities.Customers;
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
    public decimal PrescribedQuantity { get; set; }
    public decimal QuantityNeeded { get; set; }
    public decimal QuantityOnHand { get; set; }
    public MaterialRequisitionStatus Status { get; set; }
    public Guid StorageWarehouseId { get; set; }
    public Guid ProductionWarehouseId { get; set; }
    public MaterialDepartmentDetails MaterialDepartment { get; set; }
    public decimal FrozenQuantity { get; set; }
    public decimal TotalFrozenQuantity { get; set; }
    public decimal ConsumedQuantity { get; set; }
    public decimal ExtraQuantity { get; set; }
    public bool IsSubstitute { get; set; }
    public MaterialDto ParentMaterial { get; set; }
    /// <summary>
    /// Why this substitute cannot be chosen for the batch (no quantity of its own on the BOM, or not linked to the
    /// department). Null when it can be chosen. Every substitute on the BOM is listed, so production sees it either way.
    /// </summary>
    public string SubstituteUnavailableReason { get; set; }
}

public class ForecastMaterialDto
{
    public MaterialDto Material { get; set; }
    public decimal QuantityNeeded { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal ExpiredQuantity { get; set; }
    /// <summary>Usable stock held for other batches whose stock has not been issued yet.</summary>
    public decimal ReservedQuantity { get; set; }
    /// <summary>In-date stock that is not released for use (quarantined, frozen, rejected) or whose unit cannot be read.</summary>
    public decimal? UnavailableQuantity { get; set; }
    /// <summary>Approved, in-date stock: on hand less expired and unavailable.</summary>
    public decimal? UsableQuantity { get; set; }
    /// <summary>Usable stock not held for other batches: what a new batch could reserve.</summary>
    public decimal? FreeQuantity { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    /// <summary>
    /// A substitute that may be used in place of <see cref="SubstituteFor"/>, listed right after that main material with
    /// its own quantity and stock. Production chooses main or substitute when it starts the batch.
    /// </summary>
    public bool IsSubstitute { get; set; }
    public MaterialDto SubstituteFor { get; set; }
    /// <summary>Why this substitute cannot be used (no quantity of its own on the BOM); null when it can.</summary>
    public string SubstituteUnavailableReason { get; set; }
    /// <summary>
    /// Whether the free stock (usable, not reserved for other batches) covers what is needed: the same rule production
    /// applies when a batch starts. Stock reserved for other batches is theirs, so a forecast never counts it as available.
    /// </summary>
    public bool IsAvailable => (FreeQuantity ?? Math.Max(QuantityOnHand - ExpiredQuantity - ReservedQuantity, 0)) >= QuantityNeeded;
}

public class ProductionScheduleProcurementPackageDto
{
    public MaterialDto Material { get; set; }
    public MaterialDto DirectLinkMaterial { get; set; }
    public decimal BaseQuantity { get; set; }
    public decimal PrescribedQuantity { get; set; }
    public decimal QuantityNeeded { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal UnitCapacity { get; set; }
    public MaterialRequisitionStatus Status { get; set; }
    public decimal PackingExcessMargin { get; set; }
    public Guid StorageWarehouseId { get; set; }
    public Guid ProductionWarehouseId { get; set; }
    public MaterialDepartmentDetails MaterialDepartment { get; set; }
    public decimal FrozenQuantity { get; set; }
    public decimal TotalFrozenQuantity { get; set; }
    public decimal ConsumedQuantity { get; set; }
    public decimal ExtraQuantity { get; set; }
    public bool IsSubstitute { get; set; }
    public MaterialDto ParentMaterial { get; set; }
    /// <summary>
    /// Why this substitute cannot be chosen for the batch (no quantity of its own on the BOM, or not linked to the
    /// department). Null when it can be chosen. Every substitute on the BOM is listed, so production sees it either way.
    /// </summary>
    public string SubstituteUnavailableReason { get; set; }
}

public class MaterialDepartmentDetails
{
    public CollectionItemDto Department { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal Density { get; set; }
    public decimal ReOrderLevel { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public decimal MaximumStockLevel { get; set; }
}

public class ProductionScheduleProductDto
{
    public Guid Id { get; set; }
    public CollectionItemDto ProductionSchedule { get; set; }
    public ProductListDto Product { get; set; }
    public decimal Quantity { get; set; }
    public string BatchNumber { get; set; }
    public BatchSize BatchSize { get; set; }
    public CustomerDto MarketType { get; set; }
    public bool Cancelled { get; set; }
    public string ReasonForCancellation { get; set; }
    public ProductPackingDto ProductPacking { get; set; }
    public bool HasStarted { get; set; }
    public Guid? ProductionActivityId { get; set; }
}

public enum MaterialRequisitionStatus
{
    None = 0,
    StockTransfer = 1,
    PurchaseRequisition = 2,
    Local = 3,
    Foreign = 4,
    StockRequisition = 5,
    Issued = 6,
    InHouse = 7,
    Supplied = 8,
}
