using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Warehouses;

namespace DOMAIN.Entities.Materials.Batch;

public class FinishedGoodsTransferDto : BaseDto
{
    public string TransferNoteNumber { get; set; }
    public Warehouse FromWarehouse { get; set; }
    public Guid? ToWarehouseId { get; set; }
    public Warehouse ToWarehouse { get; set; }
    public decimal QuantityPerPack { get; set; }
    public Guid? PackageStyleId { get; set; }
    public PackageStyle PackageStyle { get; set; }
    public Guid? UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal TotalQuantity { get; set; }
    public string QarNumber { get; set; }
    public Guid BatchManufacturingRecordId { get; set; }
    public BatchManufacturingRecord BatchManufacturingRecord { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public ProductionActivityStep ProductionActivityStep { get; set; }
}