using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Employees;

public class Employee : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; }
    public string StaffNumber { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public EmployeeType Type { get; set; } 
    public List<Sibling> Siblings { get; set; }
}

public class Sibling : BaseEntity
{
    public string Name { get; set; }
}

public enum EmployeeType
{
    Permanent,
    Casual
}