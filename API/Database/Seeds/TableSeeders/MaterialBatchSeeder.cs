/*
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Base;
using INFRASTRUCTURE.Context;
using DOMAIN.Entities.Materials.Batch;

namespace API.Database.Seeds.TableSeeders
{
    public class MaterialBatchSeeder : ISeeder
    {
        public void Handle(IServiceScope scope)
        {
            var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
            SeedUnitOfMeasures(dbContext); // Ensure unit of measures are seeded before materials
            SeedMaterialAndBatches(dbContext);
        }

        private void SeedUnitOfMeasures(ApplicationDbContext dbContext)
        {
            if (dbContext.UnitOfMeasures.Any()) return; // Prevent re-seeding if already present
            
            foreach (var unit in UnitOfMeasureUtils.All())
            {
                dbContext.UnitOfMeasures.Add(new UnitOfMeasure
                {
                    Name = unit.Name,
                    Description = unit.Description,
                    Symbol = unit.Symbol,
                    IsScalable = unit.IsScalable,
                    IsRawMaterial = unit.IsRawMaterial
                });
            }
            dbContext.SaveChanges();
        }

        private void SeedMaterialAndBatches(ApplicationDbContext dbContext)
        {
            // Get some existing warehouse locations to move the material to
            var warehouseLocations = dbContext.WarehouseLocations.Take(2).ToList();
            if (warehouseLocations.Count == 0)
            {
                return;
            }
            
            // Check if the material already exists
            var existingMaterial = dbContext.Materials.FirstOrDefault(m => m.Name == "Paracetamol 500mg Tablet");
            if (existingMaterial is not null) return;
            
            // Seeding a valid material
            var testMaterial = new Material
            {
                Name = "Paracetamol 500mg Tablet",
                Code = "M-001",
                Description = "Paracetamol 500mg tablet for pain relief.",
                MinimumStockLevel = 100,
                MaximumStockLevel = 20000
            };

            dbContext.Materials.Add(testMaterial);
            dbContext.SaveChanges(); // Save to ensure we have the MaterialId

            // Get unit of measures to link with batches
            var kgUoM = dbContext.UnitOfMeasures.First(u => u.Name == "Kilogram").Id;
            var litreUoM = dbContext.UnitOfMeasures.First(u => u.Name == "Litre").Id;

            // Seeding multiple material batches for the test material
            var batches = new[]
            {
                new MaterialBatch
                {
                    Code = "MB-001",
                    MaterialId = testMaterial.Id,
                    TotalQuantity = 500,
                    UoMId = kgUoM,
                    Status = BatchStatus.Available,
                    DateReceived = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddYears(1)
                },
                new MaterialBatch
                {
                    Code = "MB-002",
                    MaterialId = testMaterial.Id,
                    TotalQuantity = 300,
                    UoMId = litreUoM,
                    Status = BatchStatus.Available,
                    DateReceived = DateTime.UtcNow.AddDays(-5),
                    ExpiryDate = DateTime.UtcNow.AddYears(1)
                },
                new MaterialBatch
                {
                    Code = "MB-003",
                    MaterialId = testMaterial.Id,
                    TotalQuantity = 200,
                    UoMId = kgUoM,
                    Status = BatchStatus.Available,
                    DateReceived = DateTime.UtcNow.AddDays(-10),
                    ExpiryDate = DateTime.UtcNow.AddYears(1)
                }
            };

            dbContext.MaterialBatches.AddRange(batches);
            dbContext.SaveChanges();
            
            var userId = dbContext.Users.First(u => u.Email == "dkadusei@kumateck.com").Id;
            
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
                    MovedById = userId, // Simulate a user ID
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
*/
