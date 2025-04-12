using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Employees;

public class AssignEmployeeDto
{
    [Required] public Guid DesignationId { get; set; }
    
    [Required] public Guid DepartmentId { get; set; }
    
    [Required] public Guid ReportingManagerId {get; set;}
    
}