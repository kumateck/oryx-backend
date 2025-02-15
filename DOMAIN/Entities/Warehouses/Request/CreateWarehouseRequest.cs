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
    [StringLength(255)] public string FloorName { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public List<CreateWarehouseLocationRackRequest> Racks { get; set; } = [];
}

public class CreateWarehouseLocationRackRequest 
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public List<CreateWarehouseLocationShelfRequest> Shelves { get; set; } = [];
}

public class CreateWarehouseLocationShelfRequest 
{
    [StringLength(255)] public string Code { get; set; }
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
}

public class UpdateArrivalLocationRequest
{
    public Guid Id { get; set; }
    [StringLength(255)] public string Name { get; set; }
    [StringLength(255)] public string FloorName { get; set; }
    [StringLength(255)] public string Description { get; set; }
}

public class CreateArrivalLocationRequest
{
    public Guid WarehouseId { get; set; }
    [StringLength(255)] public string Name { get; set; }
    [StringLength(255)] public string FloorName { get; set; }
    [StringLength(255)] public string Description { get; set; }
}