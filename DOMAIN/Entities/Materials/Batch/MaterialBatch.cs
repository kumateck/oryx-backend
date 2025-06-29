using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;

namespace DOMAIN.Entities.Materials.Batch;

public class MaterialBatch : BaseEntity
{
    [StringLength(10000)] public string Code { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid? ChecklistId { get; set; }
    public Checklist Checklist { get; set; }
    [StringLength(10000)] public string BatchNumber { get; set; }
    public Guid? GrnId { get; set; }
    public Grn Grn { get; set; }
    public Guid? StockTransferSourceId { get; set; }
    public StockTransferSource StockTransferSource { get; set; } 
    public int NumberOfContainers { get; set; }
    public Guid? ContainerPackageStyleId { get; set; }
    public PackageStyle ContainerPackageStyle { get; set; }
    public decimal QuantityPerContainer { get; set; }
    public decimal QuantityAssigned { get; set; }
    public decimal TotalQuantity { get; set; }  
    public decimal ConsumedQuantity { get; set; }  
    public decimal RemainingQuantity => TotalQuantity - ConsumedQuantity - ReservedQuantity;
    public decimal QuantityUnassigned => RemainingQuantity - QuantityAssigned;
    public Guid? UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public BatchStatus Status { get; set; }  
    public DateTime DateReceived { get; set; }
    public DateTime? DateApproved { get; set; }
    public DateTime? DateRejected { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? RetestDate { get; set; }
    public List<Sr> SampleWeights { get; set; } = [];
    public List<MaterialBatchEvent> Events { get; set; } = [];
    public List<MassMaterialBatchMovement> MassMovements { get; set; } = [];
    public List<MaterialBatchReservedQuantity> ReservedQuantities { get; set; } = [];
    public decimal ReservedQuantity => ReservedQuantities.Sum(r => r.Quantity);
}

public class Sr:BaseEntity
{
    public Guid MaterialBatchId { get; set; }
    public MaterialBatch MaterialBatch { get; set; }
    [StringLength(10000)] public string SrNumber { get; set; }
    public decimal GrossWeight { get; set; }
    public Guid? UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
}

public class SrDto
{
    public string SrNumber { get; set; }
    public decimal GrossWeight { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
}

public enum BatchStatus
{
    Received = 0,
    Quarantine = 1,
    Testing = 2,
    Available = 3,
    Rejected = 4,
    Retest = 5,
    Frozen = 6,
    Consumed = 7,
    Approved = 8,
    TestTaken = 9,
    Checked =  10,
}

public class MaterialBatchEvent : BaseEntity
{
    public Guid BatchId { get; set; }            
    public MaterialBatch Batch { get; set; }     
    public decimal Quantity { get; set; }     
    public Guid UserId { get; set; }       
    public User User { get; set; } 
    public EventType Type { get; set; }
    public Guid? ConsumptionWarehouseId { get; set; }
    public Warehouse ConsumptionWarehouse { get; set; }
    public DateTime? ConsumedAt { get; set; }
}

public class FinishedProductBatchEvent : BaseEntity
{
    public Guid BatchId { get; set; }            
    public Product Batch { get; set; }     
    public decimal Quantity { get; set; }     
    public Guid UserId { get; set; }       
    public User User { get; set; } 
    public EventType Type { get; set; }
    public Guid? ConsumptionWarehouseId { get; set; }
    public Warehouse ConsumptionWarehouse { get; set; }
    public DateTime? ConsumedAt { get; set; }
}

public enum EventType
{
    Supplied,
    Added,
    Moved,
    Consumed
}

public class MassMaterialBatchMovement : BaseEntity
{
    public Guid BatchId { get; set; }
    public MaterialBatch Batch { get; set; }
    public Warehouse FromWarehouse { get; set; }
    public Guid? FromWarehouseId { get; set; }
    public Warehouse ToWarehouse { get; set; }
    public Guid? ToWarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime MovedAt { get; set; }
    public Guid MovedById { get; set; }
    public User MovedBy { get; set; }
    public MovementType MovementType { get; set; }  
}

public class FinishedProductBatchMovement : BaseEntity
{
    public Guid BatchId { get; set; }
    public Product Batch { get; set; }
    public Warehouse FromWarehouse { get; set; }
    public Guid? FromWarehouseId { get; set; }
    public Warehouse ToWarehouse { get; set; }
    public Guid? ToWarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime MovedAt { get; set; }
    public Guid MovedById { get; set; }
    public User MovedBy { get; set; }
    public MovementType MovementType { get; set; }  
}

public class FinishedGoodsTransferNote:BaseEntity
{
    public string TransferNoteNumber { get; set; }
    public Guid? FromWarehouseId { get; set; }
    public Warehouse FromWarehouse { get; set; }
    public Guid? ToWarehouseId { get; set; }
    public Warehouse ToWarehouse { get; set; }
    public decimal QuantityPerPack { get; set; }
    public Guid? PackageStyleId { get; set; }
    public PackageStyle PackageStyle { get; set; }
    public Guid? UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public bool IsApproved { get; set; }
    public decimal TotalQuantity { get; set; }
    [StringLength(1000)] public string QarNumber { get; set; }
    public Guid BatchManufacturingRecordId { get; set; }
    public BatchManufacturingRecord BatchManufacturingRecord { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public ProductionActivityStep ProductionActivityStep { get; set; }
}

public class FinishedGoodsTransferNoteDto : BaseDto
{
    public string TransferNoteNumber { get; set; }
    public WarehouseDto FromWarehouse { get; set; }
    public WarehouseDto ToWarehouse { get; set; }
    public decimal QuantityPerPack { get; set; }
    public PackageStyleDto PackageStyle { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal TotalQuantity { get; set; }
    public string QarNumber { get; set; }
    public BatchManufacturingRecordDto BatchManufacturingRecord { get; set; }
    public ProductionActivityStepDto ProductionActivityStep { get; set; }
}

public class MaterialBatchReservedQuantity : BaseEntity
{
    public Guid MaterialBatchId { get; set; }
    public MaterialBatch MaterialBatch { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; }
    public Guid ProductionScheduleId { get; set; }
    public ProductionSchedule ProductionSchedule { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public Guid? UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal Quantity { get; set; }
}


public enum MovementType
{
    ToWarehouse,
    ToProduction,
    BetweenLocations
}

