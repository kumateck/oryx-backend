using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Employees;

namespace DOMAIN.Entities.Children;

public class ChildDto
{
    [StringLength(100)] public string FullName { get; set; }
    
    public DateTime DateOfBirth { get; set; }
    
    public Gender Gender { get; set; }
}