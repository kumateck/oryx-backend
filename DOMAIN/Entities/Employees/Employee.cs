using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Employees;

public class Employee : BaseEntity
{
    [StringLength(200)] public string FullName { get; set; }
    
    public DateTime DateOfBirth { get; set; }
    
    public Gender Gender { get; set; }
    
    [StringLength(200)] public string ResidentialAddress { get; set; }

    [StringLength(100)] public string Nationality { get; set; }
    
    [StringLength(100)] public string Region { get; set; }
    
    public MaritalStatus MaritalStatus { get; set; }
    
    public Religion Religion { get; set; }

    [StringLength(100)] public string StaffNumber { get; set; }
    [StringLength(100)] public string Email { get; set; }
    
    [StringLength(10)] public string PhoneNumber { get; set; }
    
    [StringLength(100)] public string BankAccountNumber { get; set; }
    
    [StringLength(20)] public string SsnitNumber { get; set; }
    
    [StringLength(100)] public string GhanaCardNumber { get; set; }
    
    public EmployeeType Type { get; set; } 
    
    public ICollection<Family> FamilyInformation { get; set; }
    public ICollection<Education> EducationBackground { get; set; }
    public ICollection<EmploymentHistory> EmploymentHistory { get; set; }

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

public enum Gender
{
    Male,
    Female
}

public enum MaritalStatus
{
    Single,
    Married
}



public enum Religion
{
    Christianity,
    Islam,
    Hinduism,
    Buddhism,
    Judaism,
    TraditionalAfricanReligions,
    Sikhism,
    Bahai,
    Other
}