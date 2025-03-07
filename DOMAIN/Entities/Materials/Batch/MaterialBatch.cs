using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Products;
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
    public int NumberOfContainers { get; set; }
    public Guid? ContainerUoMId { get; set; }
    public UnitOfMeasure ContainerUoM { get; set; }
    public decimal QuantityPerContainer { get; set; }
    public decimal QuantityAssigned { get; set; }
    public decimal TotalQuantity { get; set; }  
    public decimal ConsumedQuantity { get; set; }  
    public decimal RemainingQuantity => TotalQuantity - ConsumedQuantity;
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
    //public bool IsFrozen { get; set; }  
    public List<Sr> SampleWeights { get; set; } = [];
    public List<MaterialBatchEvent> Events { get; set; } = [];
    public List<MassMaterialBatchMovement> MassMovements { get; set; } = [];
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

public enum BatchStatus
{
    Received,
    Quarantine,
    Testing,
    Available,
    Rejected,
    Retest,
    Frozen,
    Consumed,
    Approved
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
    public FinishedProduct Batch { get; set; }     
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

public enum MovementType
{
    ToWarehouse,
    ToProduction,
    BetweenLocations
}

