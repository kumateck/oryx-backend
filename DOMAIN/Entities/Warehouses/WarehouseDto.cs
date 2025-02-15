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
    public Guid WarehouseId { get; set; }
    public string Name { get; set; }
    public string FloorName { get; set; }
    public string Description { get; set; }
    public List<DistributedRequisitionMaterialDto> DistributedRequisitionMaterials { get; set; } = new();
}

public class DistributedRequisitionMaterialDto
{
    public Guid Id { get; set; }
    public Guid RequisitionItemId { get; set; }
    public Guid MaterialId { get; set; }
    public string MaterialName { get; set; }
    public Guid? UomId { get; set; }
    public string UomName { get; set; }
    public decimal Quantity { get; set; }
    public bool ConfirmArrival { get; set; }
}

public class WarehouseLocationShelfDto
{
    public Guid Id { get; set; }
    public WareHouseLocationRackDto WarehouseLocationRack { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
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