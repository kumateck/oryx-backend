using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Requisitions;

namespace DOMAIN.Entities.Warehouses;

public class Warehouse : BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public List<WarehouseLocation> Locations { get; set; } = [];
    public WarehouseArrivalLocation ArrivalLocation { get; set; }
    public WarehouseType Type { get; set; }
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

public class DistributedRequisitionMaterial:BaseEntity
{
    public Guid RequisitionItemId { get; set; }
    public Guid WarehouseArrivalLocationId{get;set;}
    public RequisitionItem RequisitionItem { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid? UomId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal Quantity { get; set; }
    public bool ConfirmArrival { get; set; }
    public DateTime? ArrivedAt { get; set; }
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
}
public enum WarehouseType
{
    Storage, 
    Production
}