using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.CompanyWorkingDays;

public class CompanyWorkingDaysDto : BaseDto
{
    public DayOfWeek Day { get; set; }
    
    public bool IsWorkingDay { get; set; }
    
    public string StartTime { get; set; }
    
    public string EndTime { get; set; }
}