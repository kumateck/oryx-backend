using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Employees;

namespace DOMAIN.Entities.Children;

public class ChildDto
{
    [StringLength(100)] [Required] public string FullName { get; set; }
    
    [Required] public DateTime DateOfBirth { get; set; }
    
    [Required] public Gender Gender { get; set; }
}