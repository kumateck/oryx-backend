using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Employees;

public class AssignEmployeeDto
{
    [Required] public Guid DesignationId { get; set; }
    
    [Required] public Guid DepartmentId { get; set; }
    
    public string StaffNumber { get; set; }
    
    public EmployeeLevel EmployeeLevel { get; set; }
    
    public DateTime StartDate { get; set; }
    
    [Required] public Guid ReportingManagerId {get; set;}
    
}