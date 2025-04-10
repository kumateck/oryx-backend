#nullable enable
using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Children;
using DOMAIN.Entities.EducationHistories;
using DOMAIN.Entities.EmergencyContacts;
using DOMAIN.Entities.EmployeeHistories;
using DOMAIN.Entities.Persons;

namespace DOMAIN.Entities.Employees;

public class Employee : BaseEntity
{
    [StringLength(200)] public string FullName { get; set; }
    
    public DateTime DateOfBirth { get; set; }
    
    public Gender Gender { get; set; }
    
    [StringLength(200)] public string ResidentialAddress { get; set; }

    [StringLength(100)] public string Nationality { get; set; }
    
    [StringLength(100)] public string Region { get; set; }
    
    public MaritalStatus MaritalStatus { get; init; }
    
    public Religion Religion { get; set; }

    [StringLength(100)] public string? StaffNumber { get; set; }
    [StringLength(100)] public string Email { get; set; }
    
    [StringLength(10)] public string PhoneNumber { get; set; }
    
    [StringLength(100)] public string BankAccountNumber { get; set; }
    
    [StringLength(20)] public string SsnitNumber { get; set; }
    
    [StringLength(100)] public string GhanaCardNumber { get; set; }
    
    public EmployeeType Type { get; set; } 
    
    public Person Mother { get; set; }
    
    public Person Father { get; set; }
    
    public Person Spouse { get; set; }
    
    public EmergencyContact EmergencyContact { get; set; }
    
    public EmergencyContact NextOfKin { get; set; }
    
    public ICollection<Child>? Children { get; set; }
    public ICollection<Education> EducationBackground { get; set; }
    public ICollection<EmploymentHistory> EmploymentHistory { get; set; }

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