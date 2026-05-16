using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.PayrollCalendars;

public class PayrollCalendarDto : BaseDto
{
    public string Name { get; set; }
    public PayrollFrequency Frequency { get; set; }
    public int CutoffOffsetDays { get; set; }
    public int PaymentOffsetDays { get; set; }
    public DayOfWeek? WeekStartsOn { get; set; }
    public bool IsActive { get; set; }
    public CollectionItemDto PayrollCompany { get; set; }
}