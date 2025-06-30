using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.Warehouses;
using SHARED;

namespace DOMAIN.Entities.Materials;

/// <summary>
/// This class is meant to hold transfer of material batches between warehouses before they assign to their shelves
/// </summary>
public class HoldingMaterialTransfer : BaseEntity
{
    [StringLength(1000)] public string ModelType { get; set; }
    public Guid? StockTransferId { get; set; }
    public StockTransfer StockTransfer { get; set; }
    public HoldingMaterialTransferStatus Status { get; set; }
    public List<HoldingMaterialTransferBatch> Batches { get; set; } = [];
}

public class HoldingMaterialTransferBatch
{
    public Guid Id { get; set; }
    public Guid HoldingMaterialTransferId { get; set; }
    public HoldingMaterialTransfer HoldingMaterialTransfer { get; set; }
    public Guid MaterialBatchId { get; set; }
    public MaterialBatch MaterialBatch { get; set; }
    public Guid? UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal Quantity { get; set; }
    public Guid SourceWarehouseId {get; set; }
    public Warehouse SourceWarehouse { get; set; }
    public Guid DestinationWarehouseId { get; set; }
    public Warehouse DestinationWarehouse { get; set; }
}

public enum HoldingMaterialTransferStatus
{
    Pending,
    Transferred
}

public class HoldingMaterialTransferDto
{
    public MaterialDto Material { get; set; }
    public HoldingMaterialTransferStatus Status { get; set; }
    public List<HoldingMaterialTransferBatchDto> Batches { get; set; } = [];
}

public class HoldingMaterialTransferBatchDto
{
    public MaterialBatchDto MaterialBatch { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal Quantity { get; set; }
    public CollectionItemDto SourceWarehouse { get; set; }
    public CollectionItemDto DestinationWarehouse { get; set; }
}