using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Base;
using INFRASTRUCTURE.Context;
using System;
using System.Linq;
using DOMAIN.Entities.Materials.Batch;

namespace API.Database.Seeds.TableSeeders
{
    public class MaterialBatchSeeder : ISeeder
    {
        public void Handle(IServiceScope scope)
        {
            var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
            SeedMaterialAndBatches(dbContext);
        }

        private void SeedMaterialAndBatches(ApplicationDbContext dbContext)
        {
            var existingMaterial = dbContext.Materials.FirstOrDefault(m => m.Name == "Test Material Warehouse Location");
            if (existingMaterial is not null) return;
            
            // Seeding a test material
            var testMaterial = new Material
            {
                Name = "Test Material Warehouse Location",
                Description = "This is a test material for seeding.",
                MinimumStockLevel = 100,
                MaximumStockLevel = 20000
            };

            dbContext.Materials.Add(testMaterial);
            dbContext.SaveChanges(); // Save to ensure we have the MaterialId

            // Seeding multiple material batches for the test material
            var batches = new[]
            {
                new MaterialBatch
                {
                    Code = "MB-001",
                    MaterialId = testMaterial.Id,
                    TotalQuantity = 500,
                    UoMId = dbContext.UnitOfMeasures.FirstOrDefault()?.Id ?? Guid.NewGuid(),
                    Status = BatchStatus.Available,
                    DateReceived = DateTime.UtcNow,
                    CurrentLocationId = dbContext.WarehouseLocations.FirstOrDefault()?.Id ?? Guid.NewGuid()
                },
                new MaterialBatch
                {
                    Code = "MB-002",
                    MaterialId = testMaterial.Id,
                    TotalQuantity = 300,
                    UoMId = dbContext.UnitOfMeasures.FirstOrDefault()?.Id ?? Guid.NewGuid(),
                    Status = BatchStatus.Quarantine,
                    DateReceived = DateTime.UtcNow.AddDays(-5),
                    CurrentLocationId = dbContext.WarehouseLocations.FirstOrDefault()?.Id ?? Guid.NewGuid()
                },
                new MaterialBatch
                {
                    Code = "MB-003",
                    MaterialId = testMaterial.Id,
                    TotalQuantity = 200,
                    UoMId = dbContext.UnitOfMeasures.FirstOrDefault()?.Id ?? Guid.NewGuid(),
                    Status = BatchStatus.Testing,
                    DateReceived = DateTime.UtcNow.AddDays(-10),
                    CurrentLocationId = dbContext.WarehouseLocations.FirstOrDefault()?.Id ?? Guid.NewGuid()
                }
            };

            dbContext.MaterialBatches.AddRange(batches);
            dbContext.SaveChanges();
        }
    }
}
