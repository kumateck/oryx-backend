using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.PayrollCalendars;

public class CreatePayrollCalendarRequest
{
    [Required, StringLength(100)] public string Name { get; set; }
    [Required] public PayrollFrequency Frequency { get; set; }
    [Range(0, 31)] public int CutoffOffsetDays { get; set; }
    [Range(0, 31)] public int PaymentOffsetDays { get; set; }
    public DayOfWeek? WeekStartsOn { get; set; }
    [Required] public Guid PayrollCompanyId { get; set; }
}