using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Children;
using DOMAIN.Entities.EducationHistories;
using DOMAIN.Entities.EmergencyContacts;
using DOMAIN.Entities.EmployeeHistories;
using DOMAIN.Entities.Persons;


namespace DOMAIN.Entities.Employees;

public class CreateEmployeeRequest
{
    [Required] public string FullName { get; set; }

    [Required] public DateTime DateOfBirth { get; set; }

    [Required] public Gender Gender { get; set; }

    [Required] public string Contact { get; set; }

    public string Region { get; set; }
    
    public string Nationality { get; set; }
    
    public string ResidentialAddress { get; set; }

    [Required] public MaritalStatus MaritalStatus { get; set; }

    [Required] public Religion Religion { get; set; }

    [Required] public DateTime DateEmployed { get; set; }
    
    [StringLength(100)] public string BankAccountNumber { get; set; }
    
    [StringLength(20)] public string SsnitNumber { get; set; }
    
    [StringLength(100)] public string GhanaCardNumber { get; set; }

    public string StaffNumber { get; set; }
    
    public string Email { get; set; }
    
    public Person Mother { get; set; }
    
    public Person Father { get; set; }
    
    public Person Spouse { get; set; }
    
    public EmergencyContact EmergencyContact { get; set; }
    
    public EmergencyContact NextOfKin { get; set; }
    
    public List<ChildDto>? Children { get; set; }
    
    public List<EducationDto> EducationBackground { get; set; }
    
    public List<EmploymentHistoryDto> EmploymentHistory { get; set; }
    
}