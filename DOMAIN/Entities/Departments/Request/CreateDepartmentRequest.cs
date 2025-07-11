using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Warehouses.Request;

namespace DOMAIN.Entities.Departments.Request;

public class CreateDepartmentRequest
{
    [StringLength(100)] public string Code { get; set; }
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public DepartmentType Type { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public List<CreateWarehouseRequest> Warehouses { get; set; } = [];
}