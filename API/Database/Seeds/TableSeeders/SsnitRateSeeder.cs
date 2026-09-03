using DOMAIN.Entities.Payroll;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace API.Database.Seeds.TableSeeders;

/// <summary>
/// Seeds the SSNIT contribution rate effective 2026-01-01: 5.5% employee / 13% employer
/// (13.5% of the combined 18.5% funds Tier 1, the remaining 5% is the employee's Tier 2),
/// with the monthly insurable earnings ceiling in force from that date.
/// </summary>
public class SsnitRateSeeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (dbContext.SsnitRates.IgnoreQueryFilters().Any()) return;

        dbContext.SsnitRates.Add(new SsnitRate
        {
            EmployeeRate = 5.5m,
            EmployerRate = 13m,
            Tier2Rate = 5m,
            InsurableEarningsCeiling = 69_000m,
            EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        dbContext.SaveChanges();
    }
}
