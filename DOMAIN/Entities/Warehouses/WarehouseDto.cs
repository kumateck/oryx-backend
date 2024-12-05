using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Warehouses;

public class WarehouseDto 
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public WarehouseType Type { get; set; }
    public List<WarehouseLocationDto> Locations { get; set; } = [];
}

public class WarehouseLocationDto 
{ 
    public Guid Id { get; set; }
    public string Name { get; set; }
}

public class WarehouseStockDto
{
    public WarehouseDto Warehouse { get; set; }
    public int StockQuantity { get; set; }
}