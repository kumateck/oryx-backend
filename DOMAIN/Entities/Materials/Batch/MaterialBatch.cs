using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;

namespace DOMAIN.Entities.Materials.Batch;

public class MaterialBatch : BaseEntity
{
    [StringLength(10000)] public string Code { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public int TotalQuantity { get; set; }        
    public int ConsumedQuantity { get; set; }  
    public int RemainingQuantity => TotalQuantity - ConsumedQuantity;
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public BatchStatus Status { get; set; }  
    public DateTime DateReceived { get; set; }
    public DateTime? DateApproved { get; set; }
    public DateTime? DateRejected { get; set; }
    public DateTime ExpiryDate { get; set; }
    public List<MaterialBatchEvent> Events { get; set; } = [];
    public List<MaterialBatchMovement> Movements { get; set; } = [];
}

public enum BatchStatus
{
    Received,
    Quarantine,
    Testing,
    Available,
    Rejected,
    Retest
}

public class MaterialBatchEvent : BaseEntity
{
    public Guid BatchId { get; set; }            
    public MaterialBatch Batch { get; set; }     
    public int Quantity { get; set; }     
    public Guid UserId { get; set; }       
    public User User { get; set; } 
    public EventType Type { get; set; }
    // Nullable fields for tracking consumption location and timestamp
    public Guid? ConsumedLocationId { get; set; }
    public WarehouseLocation ConsumedLocation { get; set; }
    public DateTime? ConsumedAt { get; set; }
}

public enum EventType
{
    Supplied,
    Added,
    Moved,
    Consumed
}


public class MaterialBatchMovement : BaseEntity
{
    public Guid BatchId { get; set; }
    public MaterialBatch Batch { get; set; }

    public Guid? FromLocationId { get; set; }
    public WarehouseLocation FromLocation { get; set; }

    public Guid ToLocationId { get; set; }
    public WarehouseLocation ToLocation { get; set; }

    public int Quantity { get; set; }

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

