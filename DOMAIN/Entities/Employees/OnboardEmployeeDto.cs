using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Employees;

public class OnboardEmployeeDto
{
    [Required] public List<EmployeeInviteDto> EmailList { get; set; }
}

public class EmployeeInviteDto
{
    public string Name { get; set; }
    [Required, EmailAddress] public string Email { get; set; }
    [Required] public EmployeeType EmployeeType { get; set; }
    
}