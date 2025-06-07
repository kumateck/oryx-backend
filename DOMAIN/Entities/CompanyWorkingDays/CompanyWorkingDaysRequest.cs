using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.CompanyWorkingDays;

public class CompanyWorkingDaysRequest
{
    [Required] public DayOfWeek Day { get; set; }
    
    [Required] public bool IsWorkingDay { get; set; }
    [Required, StringLength(8)] public string StartTime { get; set; }
    
    [Required, StringLength(8)] public string EndTime { get; set; }
}