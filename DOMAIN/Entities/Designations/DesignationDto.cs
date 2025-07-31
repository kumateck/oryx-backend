using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Designations;

public class DesignationDto: BaseDto
{
    public string Name { get; set; }
    
    public string Description { get; set; }
    
    public int MaximumLeaveDays { get; set; }

    public List<DepartmentDto> Departments { get; set; } 
    
}

public class DesignationDepartmentDto: DesignationDto
{
    public Guid ReportingManagerId { get; set; }
    public UserDto ReportingManager { get; set; }
}