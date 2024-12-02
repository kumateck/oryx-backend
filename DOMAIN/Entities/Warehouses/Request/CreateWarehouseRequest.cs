using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Warehouses.Request;

public class CreateWarehouseRequest
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public WarehouseType Type { get; set; }
    public List<CreateWarehouseLocationRequest> Locations { get; set; } = [];
}

public class CreateWarehouseLocationRequest 
{
    [StringLength(255)] public string Name { get; set; }
}