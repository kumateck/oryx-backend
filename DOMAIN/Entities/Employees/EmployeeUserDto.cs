using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Employees;

public class EmployeeUserDto
{
    
    [Required] public string FullName { get; set; }
    
    [Required] [EmailAddress] public string Email { get; set; }
    
    public string Department { get; set; }
    
    public string? Role { get; set; }
    
    public string? Avatar { get; set; }
    
}