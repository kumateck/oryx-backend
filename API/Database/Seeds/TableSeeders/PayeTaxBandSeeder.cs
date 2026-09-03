using DOMAIN.Entities.Payroll;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace API.Database.Seeds.TableSeeders;

/// <summary>
/// Seeds the Ghana Revenue Authority's annual graduated PAYE bands effective 2026-01-01.
/// When GRA revises the bands, add a new effective-dated row instead of editing these.
/// </summary>
public class PayeTaxBandSeeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (dbContext.PayeTaxBands.IgnoreQueryFilters().Any()) return;

        var effectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        dbContext.PayeTaxBands.AddRange(
            new PayeTaxBand { LowerBound = 0, UpperBound = 5_880, Rate = 0, EffectiveFrom = effectiveFrom },
            new PayeTaxBand { LowerBound = 5_880, UpperBound = 7_200, Rate = 5, EffectiveFrom = effectiveFrom },
            new PayeTaxBand { LowerBound = 7_200, UpperBound = 8_760, Rate = 10, EffectiveFrom = effectiveFrom },
            new PayeTaxBand { LowerBound = 8_760, UpperBound = 46_760, Rate = 17.5m, EffectiveFrom = effectiveFrom },
            new PayeTaxBand { LowerBound = 46_760, UpperBound = 238_760, Rate = 25, EffectiveFrom = effectiveFrom },
            new PayeTaxBand { LowerBound = 238_760, UpperBound = 605_000, Rate = 30, EffectiveFrom = effectiveFrom },
            new PayeTaxBand { LowerBound = 605_000, UpperBound = null, Rate = 35, EffectiveFrom = effectiveFrom }
        );

        dbContext.SaveChanges();
    }
}
