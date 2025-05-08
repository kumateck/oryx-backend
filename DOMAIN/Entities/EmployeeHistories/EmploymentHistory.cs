using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DOMAIN.Entities.EmployeeHistories;

[Owned]
public class EmploymentHistory
{
    
    [StringLength(150)] public string CompanyName { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    [StringLength(100)] public string Position { get; set; }

}