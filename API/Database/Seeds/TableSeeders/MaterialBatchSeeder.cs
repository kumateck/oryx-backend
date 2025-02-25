using INFRASTRUCTURE.Context;
using DOMAIN.Entities.Materials.Batch;

namespace API.Database.Seeds.TableSeeders
{
    public class MaterialBatchSeeder : ISeeder
    {
        public void Handle(IServiceScope scope)
        {
            var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
            SeedMaterialBatches(dbContext);
        }

        private void SeedMaterialBatches(ApplicationDbContext dbContext)
        {
            var warehouseLocations = dbContext.WarehouseLocations.Take(2).ToList();
            if (warehouseLocations.Count == 0)
            {
                return;
            }

            var userId = dbContext.Users.First(u => u.Email == "dkadusei@kumateck.com").Id;

            var materials = new[]
            {
                new { Name = "Menthol Crystals (Race menthol)", Code = "M-001", UoM = "mg" },
                new { Name = "Propylene Glycol (Mono Propylene Glycol)", Code = "M-002", UoM = "ml" },
                new { Name = "Peppermint Oil", Code = "M-003", UoM = "mg" },
                new { Name = "Ethanol 99%", Code = "M-004", UoM = "ml" },
                new { Name = "Sorbitol Solution 70% (Non-Crystalline)", Code = "M-005", UoM = "mg" },
                new { Name = "Sodium Benzoate", Code = "M-006", UoM = "mg" },
                new { Name = "Citric Acid Monohydrate", Code = "M-007", UoM = "mg" },
                new { Name = "Saccharin Sodium", Code = "M-008", UoM = "mg" },
                new { Name = "Dextromethorphan HBR", Code = "M-009", UoM = "mg" },
                new { Name = "Sucrose BP (White Crystalline Sugar)", Code = "M-010", UoM = "mg" },
                new { Name = "Tartrazine Yellow Lake", Code = "M-011", UoM = "mg" },
                new { Name = "Phenylephrine Hydrochloride", Code = "M-012", UoM = "mg" },
                new { Name = "Citric Acid Anhydrous", Code = "M-013", UoM = "mg" },
                new { Name = "Shippers (479x435x375mm)", Code = "P-001", UoM = "piece" },
                new { Name = "Labels Alvite Syrup", Code = "P-002", UoM = "piece" },
                new { Name = "Double sided Spoon (5 & 2.5 ml) White Opaque", Code = "P-003", UoM = "piece" },
                new { Name = "Glass Amber Bottles 150ml (28 mm)", Code = "P-004", UoM = "piece" },
                new { Name = "Plastic Measuring Cups 28mm", Code = "P-005", UoM = "piece" },
                new { Name = "Shipper Labels Vital-X", Code = "P-006", UoM = "piece" },
                new { Name = "25mm white plastic Caps 30ml", Code = "P-007", UoM = "piece" },
                new { Name = "Leaflets Petogel", Code = "P-008", UoM = "piece" },
                new { Name = "Tapes BOPP (Printed) 72 mm", Code = "P-009", UoM = "piece" },
                new { Name = "28mm ROPP Caps (Metal)", Code = "P-010", UoM = "piece" }
            };

            foreach (var materialData in materials)
            {
                var existingMaterial = dbContext.Materials.FirstOrDefault(m => m.Name == materialData.Name);
                if (existingMaterial == null) continue;

                var uomId = dbContext.UnitOfMeasures.First(u => u.Symbol == materialData.UoM).Id;

                var existingBatches = dbContext.MaterialBatches
                    .Where(b => b.MaterialId == existingMaterial.Id && b.Code.StartsWith(materialData.Code))
                    .ToList();

                if (existingBatches.Count != 0) continue;

                var batches = new[]
                {
                    new MaterialBatch
                    {
                        Code = $"{materialData.Code}-B01",
                        MaterialId = existingMaterial.Id,
                        TotalQuantity = 500,
                        UoMId = uomId,
                        Status = BatchStatus.Available,
                        DateReceived = DateTime.UtcNow,
                        ExpiryDate = DateTime.UtcNow.AddYears(1)
                    },
                    new MaterialBatch
                    {
                        Code = $"{materialData.Code}-B02",
                        MaterialId = existingMaterial.Id,
                        TotalQuantity = 300,
                        UoMId = uomId,
                        Status = BatchStatus.Available,
                        DateReceived = DateTime.UtcNow.AddDays(-5),
                        ExpiryDate = DateTime.UtcNow.AddYears(1)
                    }
                };

                dbContext.MaterialBatches.AddRange(batches);
                dbContext.SaveChanges();

                foreach (var batch in batches)
                {
                    var movement = new MassMaterialBatchMovement
                    {
                        BatchId = batch.Id,
                        ToWarehouseId = warehouseLocations[0].WarehouseId,
                        Quantity = batch.TotalQuantity,
                        MovedAt = DateTime.UtcNow,
                        MovedById = userId,
                        MovementType = MovementType.ToWarehouse
                    };

                    dbContext.MassMaterialBatchMovements.Add(movement);

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
}
