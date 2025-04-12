#nullable enable
using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Children;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Designations;
using DOMAIN.Entities.EducationHistories;
using DOMAIN.Entities.EmergencyContacts;
using DOMAIN.Entities.EmployeeHistories;
using DOMAIN.Entities.Persons;
using DOMAIN.Entities.Siblings;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Employees;

public class Employee : BaseEntity
{
    [StringLength(200)] public string FullName { get; set; }
    
    public string Avatar {get; set;}
    
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
    
    [StringLength(15)] public string GhanaCardNumber { get; set; }
    
    public EmployeeType Type { get; set; } 
    
    public Person Mother { get; set; }
    
    public Person Father { get; set; }
    
    public Person Spouse { get; set; }
    
    public EmergencyContact EmergencyContact { get; set; }
    
    public EmergencyContact NextOfKin { get; set; }
    
    public ICollection<Child>? Children { get; set; }
    
    public ICollection<Sibling>? Siblings { get; set; }
    public ICollection<Education> EducationBackground { get; set; }
    public ICollection<EmploymentHistory> EmploymentHistory { get; set; }
    
    public Guid ReportingManagerId { get; set; }
    
    public User? ReportingManager { get; set; }
    
    public Guid DepartmentId { get; set; }
    
    public Department? Department { get; set; }
    
    public Guid DesignationId { get; set; }
    
    public Designation? Designation { get; set; }
    
    public DateTime StartDate { get; set; }

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