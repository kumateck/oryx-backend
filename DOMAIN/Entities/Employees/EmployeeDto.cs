using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Children;
using DOMAIN.Entities.EducationHistories;
using DOMAIN.Entities.EmergencyContacts;
using DOMAIN.Entities.EmployeeHistories;
using DOMAIN.Entities.Persons;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Employees;

public class EmployeeDto
{
    public Guid Id { get; set; } 

    [StringLength(200)] 
    public string FullName { get; set; }
        
    public DateTime DateOfBirth { get; set; }

    public Gender Gender { get; set; }

    [StringLength(200)] 
    public string ResidentialAddress { get; set; }

    [StringLength(100)] 
    public string Nationality { get; set; }

    [StringLength(100)] 
    public string Region { get; set; }

    public MaritalStatus MaritalStatus { get; set; }

    public Religion Religion { get; set; }

    [StringLength(100)] 
    public string StaffNumber { get; set; }

    [StringLength(100)] 
    public string Email { get; set; }

    [StringLength(10)] 
    public string PhoneNumber { get; set; }

    public EmployeeType Type { get; set; }

    public PersonDto Mother { get; set; }
    public PersonDto Father { get; set; }
    public PersonDto Spouse { get; set; }
    public EmergencyContactDto EmergencyContact { get; set; }
    public EmergencyContactDto NextOfKin { get; set; }

    public ICollection<ChildDto> Children { get; set; } 
    public ICollection<EducationDto> EducationBackground { get; set; }
    public ICollection<EmploymentHistoryDto> EmploymentHistory { get; set; }
}