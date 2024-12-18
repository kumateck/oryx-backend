using SHARED;

namespace DOMAIN.Entities.Departments;

public class DepartmentDto 
{ 
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<DepartmentWarehouseDto> Warehouses { get; set; }
}

public class DepartmentWarehouseDto
{
    public CollectionItemDto Warehouse { get; set; }
}