using SHARED;

namespace DOMAIN.Entities.Warehouses;

public class WarehouseDto 
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public WarehouseType Type { get; set; }
    public List<WarehouseLocationDto> Locations { get; set; } = [];
}

public class WarehouseLocationDto 
{ 
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string FloorName { get; set; }
    public string Description { get; set; }
    public CollectionItemDto Warehouse { get; set; }
    public List<WarehouseLocationRackDto> Racks { get; set; } = [];
}

public class WarehouseLocationRackDto 
{
    public Guid Id { get; set; }
    public WareHouseLocationDto WarehouseLocation { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<WarehouseLocationShelfDto> Shelves { get; set; } = [];
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
    public int StockQuantity { get; set; }
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