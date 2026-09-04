namespace DOMAIN.Entities.ShiftAssignments;

public class CreateWorkingHoursPolicyRequest
{
    public decimal MaxHoursPerDay { get; set; }
    public decimal MaxHoursPerWeek { get; set; }
    public decimal MinDailyRestHours { get; set; }
    public decimal MinWeeklyRestHours { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class WorkingHoursPolicyDto
{
    public Guid Id { get; set; }
    public decimal MaxHoursPerDay { get; set; }
    public decimal MaxHoursPerWeek { get; set; }
    public decimal MinDailyRestHours { get; set; }
    public decimal MinWeeklyRestHours { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
