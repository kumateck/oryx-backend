using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Base;
using INFRASTRUCTURE.Context;
using System;
using System.Linq;
using DOMAIN.Entities.Materials.Batch;
using Microsoft.Extensions.DependencyInjection;

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
            // Check if the material already exists
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

            // Get some existing warehouse locations to move the material to
            var warehouseLocations = dbContext.WarehouseLocations.Take(2).ToList();
            if (warehouseLocations.Count == 0)
            {
                return;
            }

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
                    // No CurrentLocationId here since we'll handle it via movement
                },
                new MaterialBatch
                {
                    Code = "MB-002",
                    MaterialId = testMaterial.Id,
                    TotalQuantity = 300,
                    UoMId = dbContext.UnitOfMeasures.FirstOrDefault()?.Id ?? Guid.NewGuid(),
                    Status = BatchStatus.Available,
                    DateReceived = DateTime.UtcNow.AddDays(-5),
                    // No CurrentLocationId here
                },
                new MaterialBatch
                {
                    Code = "MB-003",
                    MaterialId = testMaterial.Id,
                    TotalQuantity = 200,
                    UoMId = dbContext.UnitOfMeasures.FirstOrDefault()?.Id ?? Guid.NewGuid(),
                    Status = BatchStatus.Available,
                    DateReceived = DateTime.UtcNow.AddDays(-10),
                    // No CurrentLocationId here
                }
            };

            dbContext.MaterialBatches.AddRange(batches);
            dbContext.SaveChanges();

            // Now we need to add MaterialBatchMovements and MaterialBatchEvents
            foreach (var batch in batches)
            {
                // Creating a movement to simulate moving the entire batch to the warehouse
                var movement = new MaterialBatchMovement
                {
                    BatchId = batch.Id,
                    ToLocationId = warehouseLocations[0].Id, // Move to the first warehouse
                    Quantity = batch.TotalQuantity, // Move the entire batch quantity
                    MovedAt = DateTime.UtcNow,
                    MovedById = dbContext.Users.First().Id, // Simulate a user ID
                    MovementType = MovementType.ToWarehouse
                };

                dbContext.MaterialBatchMovements.Add(movement);

                // Log the movement as an event
                var batchEvent = new MaterialBatchEvent
                {
                    BatchId = batch.Id,
                    Type = EventType.Moved,
                    Quantity = movement.Quantity,
                    UserId = movement.MovedById,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.MaterialBatchEvents.Add(batchEvent);
            }

            dbContext.SaveChanges();
        }
    }
}
