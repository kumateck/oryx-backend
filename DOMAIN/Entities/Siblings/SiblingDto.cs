using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Employees;

namespace DOMAIN.Entities.Siblings;

public class SiblingDto
{
    [StringLength(100)] public string FullName { get; set; }
    
    [Required] [Phone] public string Contact { get; set; }
    
    [Required] public Gender Gender { get; set; }
}