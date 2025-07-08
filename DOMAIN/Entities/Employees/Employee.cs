using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Children;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Designations;
using DOMAIN.Entities.EducationHistories;
using DOMAIN.Entities.EmergencyContacts;
using DOMAIN.Entities.EmployeeHistories;
using DOMAIN.Entities.Persons;
using DOMAIN.Entities.ShiftAssignments;
using DOMAIN.Entities.Siblings;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Employees;

public class Employee : BaseEntity
{
    [StringLength(200)] public string FirstName { get; set; }
    [StringLength(200)] public string LastName { get; set; }
    [StringLength(1000)] public string Avatar {get; set;}
    
    public DateTime DateOfBirth { get; set; }
    
    public Gender Gender { get; set; }
    
    [StringLength(200)] public string ResidentialAddress { get; set; }

    [StringLength(100)] public string Nationality { get; set; }
    
    [StringLength(100)] public string Region { get; set; }
    
    public MaritalStatus MaritalStatus { get; init; }
    
    public Religion Religion { get; set; }

    [StringLength(100)] public string StaffNumber { get; set; }
    
    
    [StringLength(100)] public string Email { get; set; }
    
    [StringLength(10)] public string PhoneNumber { get; set; }
    
    [StringLength(100)] public string BankAccountNumber { get; set; }
    
    [StringLength(20)] public string SsnitNumber { get; set; }
    
    [StringLength(15)] public string GhanaCardNumber { get; set; }
    
    public int AnnualLeaveDays { get; set; }
    
    public EmployeeType Type { get; set; } 
    
    public Person Mother { get; set; }
    
    public Person Father { get; set; }
    
    public Person Spouse { get; set; }
    
    public EmergencyContact EmergencyContact { get; set; }
    
    public EmergencyContact NextOfKin { get; set; }

    public List<Child> Children { get; set; } = [];

    public List<Sibling> Siblings { get; set; } = [];
    public List<Education> EducationBackground { get; set; } = [];
    public List<EmploymentHistory> EmploymentHistory { get; set; } = [];
    
    public List<ShiftAssignment> ShiftAssignments { get; set; } = [];
    
    public Guid? ReportingManagerId { get; set; }
    
    public User ReportingManager { get; set; }
    
    public Guid? DepartmentId { get; set; }
    public Department Department { get; set; }
    
    public Guid? DesignationId { get; set; }
    public Designation Designation { get; set; }
    
    public DateTime DateEmployed { get; set; }
    public EmployeeLevel? Level { get; set; }
    public EmployeeStatus Status { get; set; }
    public EmployeeActiveStatus? ActiveStatus { get; set; }
    public EmployeeInactiveStatus? InactiveStatus { get; set; }
    public DateTime? SuspensionStartDate { get; set; }
    public DateTime? SuspensionEndDate { get; set; }
    public DateTime? ExitDate { get; set; }
}

public enum EmployeeLevel {
    JuniorStaff,
    SeniorStaff,
    SeniorManagement
}

public enum EmployeeStatus
{
    Active,
    Inactive,
}

public enum EmployeeActiveStatus
{
    Question = 0,
    Warning = 1,
    FinalWarning = 2,
    Suspension = 3,
}

public enum EmployeeInactiveStatus
{
    Resignation = 0,
    VacatedPost = 1,
    Deceased = 2,
    SummaryDismissed = 3,
    Termination = 4,
    Transfer = 5
}

public enum EmployeeType
{
    Casual,
    Permanent
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