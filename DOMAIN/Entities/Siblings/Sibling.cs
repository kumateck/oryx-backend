using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Employees;
using Microsoft.EntityFrameworkCore;

namespace DOMAIN.Entities.Siblings;

[Owned]
public class Sibling
{
    [StringLength(100)] public string FullName { get; set; }
    
    public string Contact { get; set; }
    
    public Gender Gender { get; set; }
}