using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
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
    public Guid WarehouseId { get; set; }  
    public Warehouse Warehouse { get; set; }
    public List<MaterialBatchEvent> Events { get; set; } = [];
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

