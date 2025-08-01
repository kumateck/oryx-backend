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
    public List<EmployeeDto> Employees { get; set; }
    
}

public class DesignationWithEmployeesDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<EmployeeWithManagerDto> Employees { get; set; } = [];
}

public class EmployeeWithManagerDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    
    public ManagerDto Manager { get; set; }

}

public class ManagerDto
{
    public Guid? ReportingManagerId { get; set; }
    public string? ReportingManagerName { get; set; }
}