using DOMAIN.Entities.ShiftAssignments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace API.Database.Seeds.TableSeeders;

/// <summary>
/// Seeds the Ghana Labour Act, 2003 (Act 651) working-hours baseline: max 8h/day,
/// 40h/week, 12h daily rest, 48h weekly rest.
/// </summary>
public class WorkingHoursPolicySeeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (dbContext.WorkingHoursPolicies.IgnoreQueryFilters().Any()) return;

        dbContext.WorkingHoursPolicies.Add(new WorkingHoursPolicy
        {
            MaxHoursPerDay = 8,
            MaxHoursPerWeek = 40,
            MinDailyRestHours = 12,
            MinWeeklyRestHours = 48,
            EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        dbContext.SaveChanges();
    }
}
