using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Warehouses;

public class WarehouseDto : BaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<WarehouseLocationDto> Locations { get; set; } = [];
}

public class WarehouseLocationDto : BaseDto
{ 
    public string Name { get; set; }
}