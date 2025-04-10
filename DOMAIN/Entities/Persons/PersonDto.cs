using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Persons;

public class PersonDto
{
    [StringLength(100)] [Required] public string FullName { get; set; }
    
    [StringLength(15)] [Required] public string PhoneNumber {get; set;}
    
    [StringLength(100)] [Required] public string Occupation { get; set; }
    
    [Required] public LifeStatus LifeStatus { get; set; }
}