using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.PayrollCompanies;

namespace DOMAIN.Entities.PayrollCalendars;

public class PayrollCalendar : BaseEntity
{
    [StringLength(100)] public string Name { get; set; }
    public PayrollFrequency Frequency { get; set; }
    public int CutoffOffsetDays { get; set; }
    public int PaymentOffsetDays { get; set; }
    public DayOfWeek? WeekStartsOn { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid PayrollCompanyId { get; set; }
    public PayrollCompany PayrollCompany { get; set; }
}

public enum PayrollFrequency
{
    Monthly = 0,
    SemiMonthly = 1,
    BiWeekly = 2,
    Weekly = 3,
    Custom = 4
}