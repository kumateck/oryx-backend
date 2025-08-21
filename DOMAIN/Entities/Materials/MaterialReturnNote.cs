using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Warehouses;
using SHARED;

namespace DOMAIN.Entities.Materials;

public class MaterialReturnNote : BaseEntity
{
    public DateTime ReturnDate { get; set; }
    public Guid ProductionScheduleId { get; set; }
    public ProductionSchedule ProductionSchedule { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    [StringLength(100)] public string BatchNumber { get; set; }
    public MaterialReturnStatus Status { get; set; }
    public List<MaterialReturnNoteFullReturn> FullReturns { get; set; } = [];
    public List<MaterialReturnNotePartialReturn> PartialReturns { get; set; } = [];
}

public class MaterialReturnNoteFullReturn : BaseEntity
{
    public Guid MaterialReturnNoteId { get; set; }
    public MaterialReturnNote MaterialReturnNote { get; set; }
    public Guid MaterialBatchReservedQuantityId { get; set; }
    public MaterialBatchReservedQuantity MaterialBatchReservedQuantity { get; set; }
    public Guid DestinationWarehouseId { get; set; }
    public Warehouse DestinationWarehouse { get; set; }
}

public class MaterialReturnNotePartialReturn : BaseEntity
{
    public Guid MaterialReturnNoteId { get; set; }
    public MaterialReturnNote MaterialReturnNote { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid? MaterialBatchId { get; set; }
    public MaterialBatch MaterialBatch { get; set; }
    public decimal Quantity { get; set; }
    public Guid? UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public Guid DestinationWarehouseId { get; set; }
    public Warehouse DestinationWarehouse { get; set; }
}

public class MaterialReturnNoteDto : BaseDto
{
    public DateTime ReturnDate { get; set; }
    public string BatchNumber { get; set; }
    public CollectionItemDto ProductionSchedule { get; set; }
    public CollectionItemDto Product { get; set; }
    public MaterialReturnStatus Status { get; set; }
    public List<MaterialReturnNoteFullReturnDto> FullReturns { get; set; } = [];
    public List<MaterialReturnNotePartialReturnDto> PartialReturns { get; set; } = [];
    public bool IsFullReturn => FullReturns.Count > 0;
}

public class MaterialReturnNoteFullReturnDto
{
    public MaterialBatchReservedQuantityDto MaterialBatchReservedQuantity { get; set; }
    public CollectionItemDto DestinationWarehouse { get; set; }
}

public class MaterialReturnNotePartialReturnDto
{
    public MaterialDto Material { get; set; }
    public MaterialBatchListDto MaterialBatch { get; set; }
    public decimal Quantity { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public CollectionItemDto DestinationWarehouse { get; set; }
}

public class PartialMaterialToReturn
{
    public Guid MaterialId { get; set; }
    public Guid? UoMId { get; set; }
    public decimal Quantity { get; set; }
}

public enum MaterialReturnStatus
{
    Pending, 
    Completed
}