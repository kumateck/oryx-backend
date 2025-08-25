using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Employees;

public class AssignEmployeeDto
{
    [Required] public Guid DesignationId { get; set; }
    
    [Required] public Guid DepartmentId { get; set; }
    
    public string StaffNumber { get; set; }
    
    [Required] public EmployeeLevel Level { get; set; }
    
    public DateTime StartDate { get; set; }
    
    [Required] public Guid ReportingManagerId {get; set;}
    
}

public class AssignEmployeeData : AssignEmployeeDto
{
    
    public UserDto ReportingManager { get; set; }

}