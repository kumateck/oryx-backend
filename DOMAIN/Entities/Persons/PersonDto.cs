using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Persons;

public class PersonDto
{
    [StringLength(100)] public string FullName { get; set; }
    
    [StringLength(15)] public string PhoneNumber {get; set;}
    
    [StringLength(100)] public string Occupation { get; set; }
    
    public LifeStatus LifeStatus { get; set; }
}