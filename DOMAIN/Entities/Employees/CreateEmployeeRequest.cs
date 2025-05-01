using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Children;
using DOMAIN.Entities.EducationHistories;
using DOMAIN.Entities.EmergencyContacts;
using DOMAIN.Entities.EmployeeHistories;
using DOMAIN.Entities.Persons;
using DOMAIN.Entities.Siblings;

namespace DOMAIN.Entities.Employees;

public class CreateEmployeeRequest
{
    public string Avatar {get; set;}
    
    [Required] [StringLength(100)] public string FullName { get; set; }
    
    [Required] public DateTime DateOfBirth { get; set; }

    [Required] public Gender Gender { get; set; }

    [Required] [Phone] public string PhoneNumber { get; set; }

    [Required] public string Region { get; set; }
    
    [Required] public string Nationality { get; set; }
    
    [Required] [StringLength(150)] public string ResidentialAddress { get; set; }

    [Required] public MaritalStatus MaritalStatus { get; set; }

    [Required] public Religion Religion { get; set; }

    [Required] public DateTime DateEmployed { get; set; }
    
    [Required] [StringLength(20)] public string BankAccountNumber { get; set; }
    
    [Required] [StringLength(20)] public string SsnitNumber { get; set; }
    
    [Required] [StringLength(15)] 
    [RegularExpression(@"^GHA-\d{9}-\d{1}$", 
        ErrorMessage = "Ghana Card number must start with 'GHA-'. " +
                       "Total length must be between 11 and 15 characters.")]
    public string GhanaCardNumber { get; set; }

    [StringLength(15)] public string StaffNumber { get; set; }
    
    [Required] [EmailAddress] public string Email { get; set; }
    
    [Required] public PersonDto Mother { get; set; }
    
    [Required] public PersonDto Father { get; set; }
    
    public PersonDto Spouse { get; set; }
    [Required] public EmergencyContactDto EmergencyContact { get; set; }
    
    [Required] public EmergencyContactDto NextOfKin { get; set; }

    public List<ChildDto> Children { get; set; } = [];

    public List<SiblingDto> Siblings { get; set; } = [];

    [Required] public List<EducationDto> EducationBackground { get; set; } = [];

    [Required] public List<EmploymentHistoryDto> EmploymentHistory { get; set; } = [];
}