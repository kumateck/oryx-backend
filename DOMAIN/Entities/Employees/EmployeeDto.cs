using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Employees;

public class EmployeeDto
{
    public Guid Id { get; set; }
    public UserDto User { get; set; }
    public string StaffNumber { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public EmployeeType Type { get; set; }
}