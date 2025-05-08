using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.CompanyWorkingDays;

public class CompanyWorkingDaysDto : BaseDto
{
    public DayOfWeek Day { get; set; }
    public bool IsWorkingDay { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}