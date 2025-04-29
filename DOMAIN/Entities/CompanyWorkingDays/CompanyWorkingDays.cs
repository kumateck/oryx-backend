using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.CompanyWorkingDays;

public class CompanyWorkingDays : BaseEntity
{
    public DayOfWeek Day { get; set; }
    public bool IsWorkingDay { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}