using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Children;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Designations;
using DOMAIN.Entities.EducationHistories;
using DOMAIN.Entities.EmergencyContacts;
using DOMAIN.Entities.EmployeeHistories;
using DOMAIN.Entities.Persons;
using DOMAIN.Entities.Siblings;

namespace DOMAIN.Entities.Employees;

public class EmployeeDto: WithAttachment
{
    public string FirstName { get; set; }
    
    public string LastName { get; set; }
        
    public string Avatar { get; set; }
    
    public DateTime DateOfBirth { get; set; }

    public Gender Gender { get; set; }
    
    public string ResidentialAddress { get; set; }
    
    public int AnnualLeaveDays { get; set; }
    
    public string Nationality { get; set; }
    
    public string BankAccountNumber { get; set; }
    
    public string SsnitNumber { get; set; }
    
    public string GhanaCardNumber { get; set; }
    
    public string Region { get; set; }

    public MaritalStatus MaritalStatus { get; set; }

    public Religion Religion { get; set; }
    
    public string StaffNumber { get; set; }
    
    public string Email { get; set; }
    
    public string PhoneNumber { get; set; }

    public EmployeeType Type { get; set; } 
    
    public DateTime DateEmployed { get; set; }
    
    public DesignationDto Designation { get; set; }
    
    public DepartmentDto Department { get; set; }

    public PersonDto Mother { get; set; }
    public PersonDto Father { get; set; }
    public PersonDto Spouse { get; set; }
    public EmergencyContactDto EmergencyContact { get; set; }
    public EmergencyContactDto NextOfKin { get; set; }

    public ICollection<ChildDto> Children { get; set; }
    public ICollection<SiblingDto> Siblings { get; set; }
    public ICollection<EducationDto> EducationBackground { get; set; }
    public ICollection<EmploymentHistoryDto> EmploymentHistory { get; set; }
}