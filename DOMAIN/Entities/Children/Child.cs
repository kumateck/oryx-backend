using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Employees;
using Microsoft.EntityFrameworkCore;

namespace DOMAIN.Entities.Children;

[Owned]
public class Child
{
    [StringLength(100)] public string FullName { get; set; }
    
    [PastDate] public DateTime DateOfBirth { get; set; }
    
    public Gender Gender { get; set; }
    
}


public class PastDateAttribute : ValidationAttribute
{
    public PastDateAttribute()
    {
        ErrorMessage = "Date of birth cannot be in the future.";
    }

    public override bool IsValid(object value)
    {
        if (value == null)
            return true; 

        if (DateTime.TryParse(value.ToString(), out var dateValue))
        {
            return dateValue <= DateTime.Today;
        }

        return false;
    }
}