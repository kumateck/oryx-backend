using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Employees;

public class OnboardEmployeeDto
{
    [Required] 
    public List<EmployeeInviteDto> EmailList { get; set; }
}

public class EmployeeInviteDto
{
    public string Email { get; set; }
    public EmployeeType EmployeeType { get; set; }
}