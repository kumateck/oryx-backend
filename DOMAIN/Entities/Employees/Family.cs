using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Employees;

public class Family
{
    public Guid Id { get; set; }
    
    [Required] [StringLength(200)] public string FullName { get; set; }
    
    [Required] [StringLength(200)] public string Occupation { get; set; }
    
    [StringLength(10)] [Phone] public string PhoneNumber { get; set; }
    
    [StringLength(20)] public string Relationship { get; set; }
    
    public LifeStatus LifeStatus { get; set; }
    
    [StringLength(100)] public string ResidentialAddress { get; set; }
    
    public Guid EmployeeId { get; set; }
    
}

public enum LifeStatus
{
    Alive,
    Deceased
}