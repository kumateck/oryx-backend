using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Employees;

public class OnboardEmployeeDto
{
    [Required] public string Email { get; set; }

    public EmployeeType EmployeeType { get; set; } = 0;
    
}