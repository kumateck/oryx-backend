using DOMAIN.Entities.Products;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace API.Database.Seeds.TableSeeders;

public class WarehouseDivisionSeeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        SeedWarehouseDivisions(dbContext);
    }

    private static void SeedWarehouseDivisions(ApplicationDbContext dbContext)
    {
        var targetWarehouses = dbContext.Warehouses
            .IgnoreQueryFilters()
            .Where(w => w.Name == "Beta Finished Goods Warehouse" ||
                        w.Name == "Non Beta Finished Goods Warehouse")
            .ToList();

        foreach (var warehouse in targetWarehouses)
        {
            if (warehouse.Name == "Beta Finished Goods Warehouse")
            {
                warehouse.Division = Division.BetaLactam;
            }
            else if (warehouse.Name == "Non Beta Finished Goods Warehouse")
            {
                warehouse.Division = Division.NonBetaLactam;
            }
        }

        dbContext.Warehouses.UpdateRange(targetWarehouses);
        dbContext.SaveChanges();
    }
}