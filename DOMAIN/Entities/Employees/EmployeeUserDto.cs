using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Employees;

public class EmployeeUserDto
{
    [Required] public Guid EmployeeId { get; set; }
    
}