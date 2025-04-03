using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Employees;

public class CreateEmployeeRequest
{
    [Required]
    public string StaffNumber { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [Phone]
    public string PhoneNumber { get; set; }

    public EmployeeType Type { get; set; } 
}