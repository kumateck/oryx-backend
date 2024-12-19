using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Departments.Request;

public class CreateDepartmentRequest
{
    [StringLength(100)] public string Code { get; set; }
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public List<CreateDepartmentWarehouseRequest> Warehouses { get; set; } = [];
}

public class CreateDepartmentWarehouseRequest
{
    public Guid WarehouseId { get; set; }
}