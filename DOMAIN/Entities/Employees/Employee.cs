using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Employees;

public class Employee : BaseEntity
{
    [StringLength(100)] public string StaffNumber { get; set; }
    [StringLength(100)] public string Email { get; set; }
    [StringLength(100)] public string PhoneNumber { get; set; }
    public EmployeeType Type { get; set; } 
    public List<Sibling> Siblings { get; set; }
}

public class Sibling : BaseEntity
{
    [StringLength(100)] public string Name { get; set; }
}

public enum EmployeeType
{
    Permanent,
    Casual
}