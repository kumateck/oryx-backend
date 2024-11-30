using DOMAIN.Entities.Base;
using DOMAIN.Entities.Warehouses;

namespace DOMAIN.Entities.Departments;

public class DepartmentDto : BaseDto
{ 
    public string Name { get; set; }
    public string Description { get; set; }
    public WarehouseDto Warehouse { get; set; }
}