using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Warehouses;

namespace DOMAIN.Entities.BinCards;

public class BinCardInformation : BaseEntity
{
    public Guid? MaterialBatchId { get; set; }
    public MaterialBatch MaterialBatch { get; set; }
    [StringLength(500)] public string Description { get; set; }
    [StringLength(500)] public string WayBill { get; set; }
    [StringLength(500)] public string ArNumber { get; set; }
    [StringLength(500)] public string Supplier { get; set; }
    [StringLength(500)] public string Manufacturer { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityIssued { get; set; }
    public decimal BalanceQuantity { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public Guid? UoMId { get; set; }
    public Product Product { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; }
    public Guid? RequisitionId { get; set; }
    [StringLength(255)] public string RequisitionCode { get; set; }
    [StringLength(255)] public string ProductBatchNumber { get; set; }
}

public class BinCardInformationDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public MaterialBatchListDto MaterialBatch { get; set; }
    public string Description { get; set; }
    public string WayBill { get; set; }
    public string ArNumber { get; set; }
    public string Supplier { get; set; }
    public string Manufacturer { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityIssued { get; set; }
    public decimal BalanceQuantity { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public ProductListDto Product { get; set; }
    public string WarehouseName { get; set; }
    public Guid? RequisitionId { get; set; }
    public string RequisitionCode { get; set; }
    public string ProductBatchNumber { get; set; }
}

public class ProductBinCardInformation : BaseEntity
{
    public Guid? BatchId { get; set; }
    public BatchManufacturingRecord Batch { get; set; }
    [StringLength(500)] public string Description { get; set; }
    [StringLength(500)] public string WayBill { get; set; }
    [StringLength(500)] public string ArNumber { get; set; }
    [StringLength(500)] public string Supplier { get; set; }
    [StringLength(500)] public string Manufacturer { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityIssued { get; set; }
    public decimal BalanceQuantity { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public Guid? UoMId { get; set; }
}

public class ProductBinCardInformationDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public BatchManufacturingRecordDto Batch { get; set; }
    public string Description { get; set; }
    public string WayBill { get; set; }
    public string ArNumber { get; set; }
    public string Supplier { get; set; }
    public string Manufacturer { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityIssued { get; set; }
    public decimal BalanceQuantity { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
}
