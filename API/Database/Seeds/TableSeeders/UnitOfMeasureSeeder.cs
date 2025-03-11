using APP.Utils;
using DOMAIN.Entities.Base;
using INFRASTRUCTURE.Context;

namespace API.Database.Seeds.TableSeeders;

public class UnitOfMeasureSeeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

        if (dbContext.UnitOfMeasures.Any()) return;

        SeedUnitOfMeasures(dbContext);
    }

    private static void SeedUnitOfMeasures(ApplicationDbContext dbContext)
    {
        var uom = new List<UnitOfMeasure>();
        
        foreach (var unit in UnitOfMeasureUtils.All())
        {
            uom.Add(new UnitOfMeasure
            {
                Name = unit.Name,
                Description = unit.Description,
                Symbol = unit.Symbol,
                IsScalable = unit.IsScalable,
                IsRawMaterial = unit.IsRawMaterial
            });
        }
        dbContext.UnitOfMeasures.AddRange(uom);
        dbContext.SaveChanges();
    }
}