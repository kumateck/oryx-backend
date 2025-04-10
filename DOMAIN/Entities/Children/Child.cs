using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Employees;
using Microsoft.EntityFrameworkCore;

namespace DOMAIN.Entities.Children;

[Owned]
public class Child
{
    [StringLength(100)] public string FullName { get; set; }
    
    public DateTime DateOfBirth { get; set; }
    
    public Gender Gender { get; set; }
    
}