using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

namespace DOMAIN.Entities.Persons;

[Owned]
public class Person
{
    
    [StringLength(100)] public string FullName { get; set; }
    
    [StringLength(15)] public string PhoneNumber {get; set;}
    
    [StringLength(100)] public string Occupation { get; set; }
    
    public LifeStatus LifeStatus { get; set; }
    
}

public enum LifeStatus
{
    Alive,
    Deceased,
}