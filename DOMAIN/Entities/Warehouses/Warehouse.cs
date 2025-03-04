using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Shipments;

namespace DOMAIN.Entities.Warehouses;

public class Warehouse : BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department Department { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public List<WarehouseLocation> Locations { get; set; } = [];
    public WarehouseArrivalLocation ArrivalLocation { get; set; }
    public WarehouseType Type { get; set; }
    public MaterialKind? MaterialKind { get; set; }
}

public class WarehouseArrivalLocation:BaseEntity
{
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; }
    [StringLength(255)] public string Name { get; set; }
    [StringLength(255)] public string FloorName { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public List<DistributedRequisitionMaterial> DistributedRequisitionMaterials { get; set; }
}

public class DistributedRequisitionMaterial : BaseEntity
{
    public Guid? RequisitionItemId { get; set; }
    public RequisitionItem RequisitionItem { get; set; }
    public Guid? WarehouseArrivalLocationId { get; set; }
    public WarehouseArrivalLocation WarehouseArrivalLocation { get; set; }
    public List<MaterialItemDistribution> MaterialItemDistributions { get; set; } = [];
    public Guid? ShipmentInvoiceId { get; set; }
    public ShipmentInvoice ShipmentInvoice { get; set; }
    public Guid? MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid? UomId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? DistributedAt { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? CheckedAt { get; set; }
    public DateTime? GrnGeneratedAt { get; set; }
    public DistributedRequisitionMaterialStatus Status { get; set; }
}

public class MaterialItemDistribution
{
    public Guid Id { get; set; }
    public Guid DistributedRequisitionMaterialId { get; set; }
    public DistributedRequisitionMaterial DistributedRequisitionMaterial { get; set; }
    public Guid ShipmentInvoiceItemId { get; set; }
    public ShipmentInvoiceItem ShipmentInvoiceItem { get; set; }
    public decimal Quantity { get; set; }
}

public enum DistributedRequisitionMaterialStatus
{
    Distributed,
    Arrived,
    Checked,
    GrnGenerated
}

public class WarehouseLocation : BaseEntity
{
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; }
    [StringLength(255)] public string Name { get; set; }
    [StringLength(255)] public string FloorName { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public List<WarehouseLocationRack> Racks { get; set; } = [];
}

public class WarehouseLocationRack : BaseEntity
{
    public Guid WarehouseLocationId { get; set; }
    public WarehouseLocation WarehouseLocation { get; set; }
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public List<WarehouseLocationShelf> Shelves { get; set; } = [];
}

public class WarehouseLocationShelf : BaseEntity
{
    public Guid WarehouseLocationRackId { get; set; }
    public WarehouseLocationRack WarehouseLocationRack { get; set; }
    [StringLength(255)] public string Code { get; set; }
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public List<ShelfMaterialBatch> MaterialBatches { get; set; } = new();
    public List<ShelfMaterialBatch> GetMaterialBatches() => MaterialBatches;
}

public class ShelfMaterialBatch:BaseEntity
{
    public Guid WarehouseLocationShelfId { get; set; }
    public WarehouseLocationShelf WarehouseLocationShelf { get; set; }
    public Guid MaterialBatchId { get; set; }
    public MaterialBatch MaterialBatch { get; set; }
    public decimal Quantity { get; set; }
    public Guid? UomId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    [StringLength(1000)] public string Note { get; set; }
}

public enum WarehouseType
{
    RawMaterialStorage, 
    PackagedStorage,
    FinishedGoodsStorage,
    Production
}