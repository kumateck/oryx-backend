using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;

namespace API.Database.Seeds.TableSeeders
{
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
            // Seeding the warehouses
            var warehouses = new[]
            {
                new Warehouse
                {
                    Name = "Syrup Warehouse",
                    Type = WarehouseType.Storage,
                    Locations =
                    [
                        new WarehouseLocation { Name = "Quarantine Room" },
                        new WarehouseLocation { Name = "Testing Room" },
                        new WarehouseLocation { Name = "Finished Goods Storage" }
                    ]
                },
                new Warehouse
                {
                    Name = "Ointment Warehouse",
                    Type = WarehouseType.Storage,
                    Locations =
                    [
                        new WarehouseLocation { Name = "Quarantine Room" },
                        new WarehouseLocation { Name = "Testing Room" },
                        new WarehouseLocation { Name = "Finished Goods Storage" }
                    ]
                },
                new Warehouse
                {
                    Name = "Tablet Warehouse",
                    Type = WarehouseType.Storage,
                    Locations =
                    [
                        new WarehouseLocation { Name = "Quarantine Room" },
                        new WarehouseLocation { Name = "Testing Room" },
                        new WarehouseLocation { Name = "Finished Goods Storage" }
                    ]
                },
                new Warehouse
                {
                    Name = "Beta Production Warehouse",
                    Type = WarehouseType.Production,
                    Locations =
                    [
                        new WarehouseLocation { Name = "Production Area" },
                        new WarehouseLocation { Name = "Packing Area" }
                    ]
                },
                new Warehouse
                {
                    Name = "Syrup Production Warehouse",
                    Type = WarehouseType.Production,
                    Locations =
                    [
                        new WarehouseLocation { Name = "Production Area" },
                        new WarehouseLocation { Name = "Packing Area" }
                    ]
                },
                new Warehouse
                {
                    Name = "Ointment Production Warehouse",
                    Type = WarehouseType.Production,
                    Locations =
                    [
                        new WarehouseLocation { Name = "Production Area" },
                        new WarehouseLocation { Name = "Packing Area" }
                    ]
                }
            };

            dbContext.Warehouses.AddRange(warehouses);
            dbContext.SaveChanges();
        }
    }
}
