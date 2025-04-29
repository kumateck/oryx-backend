using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.CompanyWorkingDays;

public class CompanyWorkingDaysRequest
{
    [Required] public DayOfWeek Day { get; set; }
    
    [Required] public bool IsWorkingDay { get; set; }
    
    [Required] public DateTime StartTime { get; set; }
    
    [Required] public DateTime EndTime { get; set; }
}