using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace DOMAIN.Entities.Warehouses;

public class CreateSwapRequest
{
    public Guid FirstWarehouseId { get; set; }
    public Guid SecondWarehouseId { get; set; }
    public List<CreateSwapShelfMaterialBatch> FirstSwapShelfMaterialBatches { get; set; } = [];
    public List<CreateSwapShelfMaterialBatch> SecondSwapShelfMaterialBatches { get; set; } = [];
    public bool QuantityIsValid =>
        FirstSwapShelfMaterialBatches.Sum(m => m.Quantity)
        == SecondSwapShelfMaterialBatches.Sum(m => m.Quantity);
    public Guid? StockRequisitionId { get; set; }
}

public class CreateSwapShelfMaterialBatch
{
    public Guid ShelfMaterialBatchId { get; set; }
    public Guid MaterialBatchId { get; set; }
    public Guid UoMId { get; set; }
    public decimal Quantity { get; set; }
}

public class SwapRequest : BaseEntity
{
    public Guid FirstWarehouseId { get; set; }
    public Warehouse FirstWarehouse { get; set; }
    public Guid SecondWarehouseId { get; set; }
    public Warehouse SecondWarehouse { get; set; }
    public List<SwapShelfMaterialBatch> FirstSwapShelfMaterialBatches { get; set; } = [];
    public List<SwapShelfMaterialBatch> SecondSwapShelfMaterialBatches { get; set; } = [];
    public SwapRequestStatus Status { get; set; }
    public Guid? ActionedById { get; set; }
    public User ActionedBy { get; set; }
    public DateTime? ActionedAt { get; set; }

    [StringLength(10000)]
    public string ActionNote { get; set; }
    public Guid? StockRequisitionId { get; set; }
    public Requisition StockRequisition { get; set; }
}

public enum SwapRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}

[Owned]
public class SwapShelfMaterialBatch
{
    public Guid ShelfMaterialBatchId { get; set; }
    public ShelfMaterialBatch ShelfMaterialBatch { get; set; }
    public Guid MaterialBatchId { get; set; }
    public MaterialBatch MaterialBatch { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal Quantity { get; set; }
}

public class SwapRequestDto
{
    public Guid Id { get; set; }
    public WarehouseWithoutLocationDto FirstWarehouse { get; set; }
    public WarehouseWithoutLocationDto SecondWarehouse { get; set; }
    public List<SwapShelfMaterialBatchDto> FirstSwapShelfMaterialBatches { get; set; } = [];
    public List<SwapShelfMaterialBatchDto> SecondSwapShelfMaterialBatches { get; set; } = [];
    public SwapRequestStatus Status { get; set; }
    public UserDto ActionedBy { get; set; }
    public DateTime? ActionedAt { get; set; }
}

public class SwapShelfMaterialBatchDto
{
    public MaterialBatchReducedDto MaterialBatch { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal Quantity { get; set; }
}
