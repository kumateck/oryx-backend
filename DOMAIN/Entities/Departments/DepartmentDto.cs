using DOMAIN.Entities.Base;
using DOMAIN.Entities.Warehouses;

namespace DOMAIN.Entities.Departments;

public class DepartmentDto : BaseDto
{ 
    public string Code { get; set; }
    public string Name { get; set; }
    public DepartmentType Type { get; set; }
    public string Description { get; set; }
    public List<WarehouseDto> Warehouses { get; set; }
}