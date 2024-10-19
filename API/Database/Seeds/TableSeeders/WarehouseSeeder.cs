using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;

namespace API.Database.Seeds.TableSeeders;

public class WarehouseSeeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        
        if (dbContext.Warehouses.Any()) return;

        SeedWarehouse(dbContext);
    }

    private void SeedWarehouse(ApplicationDbContext dbContext)
    {
        dbContext.Warehouses.AddRange([
        
            new Warehouse
            {
                Name = "Syrup Warehouse"
            },
            
            new Warehouse
            {
                Name = "Ointment Warehouse"

            },
            
            new Warehouse
            {
                Name = "Powder Warehouse"
            }
        ]);

        dbContext.SaveChanges();
    }
}