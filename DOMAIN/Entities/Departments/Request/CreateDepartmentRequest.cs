using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Departments.Request;

public class CreateDepartmentRequest
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public Guid? WarehouseId { get; set; }
}