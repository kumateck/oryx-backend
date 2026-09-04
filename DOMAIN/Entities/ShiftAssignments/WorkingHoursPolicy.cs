using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.ShiftAssignments;

/// <summary>
/// Effective-dated working-hours limits checked at shift-assignment time, following the
/// same pattern as Payroll's PayeTaxBand/SsnitRate - when the statutory limits change, add
/// a new row rather than editing an existing one so past assignments keep validating
/// against the rules that were live at the time. Seeded with the Ghana Labour Act, 2003
/// (Act 651) baseline: 8h/day, 40h/week, 12h daily rest, 48h weekly rest.
/// </summary>
public class WorkingHoursPolicy : BaseEntity
{
    public decimal MaxHoursPerDay { get; set; }
    public decimal MaxHoursPerWeek { get; set; }
    public decimal MinDailyRestHours { get; set; }
    public decimal MinWeeklyRestHours { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
