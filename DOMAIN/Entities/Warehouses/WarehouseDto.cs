using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Shipments;
using SHARED;

namespace DOMAIN.Entities.Warehouses;

public class WarehouseDto 
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public WarehouseType Type { get; set; }
    public List<CollectionItemDto> Locations { get; set; } = [];
}

public class WarehouseLocationDto 
{ 
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string FloorName { get; set; }
    public string Description { get; set; }
    public CollectionItemDto Warehouse { get; set; }
    public List<CollectionItemDto> Racks { get; set; } = [];
}

public class WarehouseLocationRackDto 
{
    public Guid Id { get; set; }
    public WareHouseLocationDto WarehouseLocation { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<CollectionItemDto> Shelves { get; set; } = [];
}

public class WarehouseArrivalLocationDto
{
    public Guid Id { get; set; }
    public WarehouseDto Warehouse { get; set; }
    public string Name { get; set; }
    public string FloorName { get; set; }
    public string Description { get; set; }
    public List<DistributedRequisitionMaterialDto> DistributedRequisitionMaterials { get; set; } = new();
}

public class DistributedRequisitionMaterialDto
{
    public Guid Id { get; set; }
    public RequisitionItemDto RequisitionItem { get; set; }
    public MaterialDto Material { get; set; }
    public UnitOfMeasureDto Uom { get; set; }
    public SupplierDto Supplier { get; set; }
    public ManufacturerDto Manufacturer { get; set; }
    public ShipmentInvoiceItemDto ShipmentInvoiceItem { get; set; }
    public ShipmentInvoiceDto ShipmentInvoice { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? CheckedAt { get; set; }
    public DateTime? DistributedAt { get; set; }
    public DateTime? GrnGeneratedAt { get; set; }
    public DistributedRequisitionMaterialStatus Status { get; set; }
}

public class WarehouseLocationShelfDto
{
    public Guid Id { get; set; }
    public WareHouseLocationRackDto WarehouseLocationRack { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<ShelfMaterialBatchDto> MaterialBatches { get; set; }
}

public class WarehouseStockDto
{
    public WarehouseDto Warehouse { get; set; }
    public decimal StockQuantity { get; set; }
}

public class WareHouseLocationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string FloorName { get; set; }
    public string Description { get; set; }
    public CollectionItemDto Warehouse { get; set; }
}

public class WareHouseLocationRackDto 
{
    public Guid Id { get; set; }
    public WareHouseLocationDto WarehouseLocation { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}

public class ShelfMaterialBatchDto
{
    public Guid Id { get; set; }
    public MaterialWarehouseLocationShelfDto WarehouseLocationShelf { get; set; }
    public MaterialBatchDto MaterialBatch { get; set; }
    public decimal Quantity { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public string Note { get; set; }
}

public class MaterialWarehouseLocationShelfDto
{
    public Guid Id { get; set; }
    public WareHouseLocationRackDto WarehouseLocationRack { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}