using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace API.Database.Seeds.TableSeeders;

public class WaterStockSeeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (dbContext == null)
            return;

        // 1. Ensure UoM exists (Milliliter)
        var milliliterUoM = dbContext.UnitOfMeasures.FirstOrDefault(u => u.Symbol == "ml");
        if (milliliterUoM == null)
        {
            milliliterUoM = new UnitOfMeasure
            {
                Name = "Milliliter",
                Symbol = "ml",
                Description = "Base unit of volume in the metric system",
                IsScalable = true,
                IsRawMaterial = true,
            };
            dbContext.UnitOfMeasures.Add(milliliterUoM);
            dbContext.SaveChanges();
        }

        // 2. Ensure Material Category exists
        var category = dbContext.MaterialCategories.FirstOrDefault(c => c.Name == "Solvent");
        if (category == null)
        {
            category = new MaterialCategory
            {
                Name = "Solvent",
                Description = "Standard raw materials",
                MaterialKind = MaterialKind.Raw,
            };
            dbContext.MaterialCategories.Add(category);
            dbContext.SaveChanges();
        }

        // 3. Ensure Water Material exists
        var water = dbContext.Materials.FirstOrDefault(m => m.Code == "RMP020");
        if (water == null)
        {
            water = new Material
            {
                Code = "RMP020",
                Name = "Water",
                Description = "Unlimited Water Supply",
                MaterialCategoryId = category.Id,
                Kind = MaterialKind.Raw,
                Status = BatchKind.Batch,
                IsUnlimited = true,
                CreatedAt = DateTime.UtcNow,
            };
            dbContext.Materials.Add(water);
            dbContext.SaveChanges();
        }
        else if (!water.IsUnlimited)
        {
            water.IsUnlimited = true;
            dbContext.Materials.Update(water);
            dbContext.SaveChanges();
        }

        // 4. Ensure a limitless Batch exists for Water
        var batch = dbContext.MaterialBatches.FirstOrDefault(b =>
            b.MaterialId == water.Id && b.BatchNumber == "WATER-UNLIMITED"
        );
        if (batch == null)
        {
            batch = new MaterialBatch
            {
                MaterialId = water.Id,
                BatchNumber = "WATER-UNLIMITED",
                TotalQuantity = 999_999_999_999_999_999_999_999.99m,
                ConsumedQuantity = 0,
                UoMId = milliliterUoM.Id,
                Status = BatchStatus.Available,
                DateReceived = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };
            dbContext.MaterialBatches.Add(batch);
            dbContext.SaveChanges();
        }

        // 5. Ensure it's in all Raw Material Storage warehouses
        var rawWarehouses = dbContext
            .Warehouses.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(w => w.Locations)
                .ThenInclude(l => l.Racks)
                    .ThenInclude(r => r.Shelves)
            .Where(w => w.Type == WarehouseType.RawMaterialStorage)
            .ToList();

        foreach (var warehouse in rawWarehouses)
        {
            // Get or create a shelf
            var shelf = warehouse
                .Locations.SelectMany(l => l.Racks)
                .SelectMany(r => r.Shelves)
                .FirstOrDefault();
            if (shelf == null)
            {
                var location = warehouse.Locations.FirstOrDefault();
                if (location == null)
                {
                    location = new WarehouseLocation
                    {
                        WarehouseId = warehouse.Id,
                        Name = "General Location",
                        CreatedAt = DateTime.UtcNow,
                    };
                    dbContext.WarehouseLocations.Add(location);
                    dbContext.SaveChanges();
                }

                var rack = location.Racks.FirstOrDefault();
                if (rack == null)
                {
                    rack = new WarehouseLocationRack
                    {
                        WarehouseLocationId = location.Id,
                        Name = "General Rack",
                        CreatedAt = DateTime.UtcNow,
                    };
                    dbContext.WarehouseLocationRacks.Add(rack);
                    dbContext.SaveChanges();
                }

                shelf = new WarehouseLocationShelf
                {
                    WarehouseLocationRackId = rack.Id,
                    Name = "General Shelf",
                    Code = "GEN-SHELF",
                    CreatedAt = DateTime.UtcNow,
                };
                dbContext.WarehouseLocationShelves.Add(shelf);
                dbContext.SaveChanges();
            }

            // Check if shelf already has the water batch
            if (
                !dbContext.ShelfMaterialBatches.Any(smb =>
                    smb.MaterialBatchId == batch.Id && smb.WarehouseLocationShelfId == shelf.Id
                )
            )
            {
                dbContext.ShelfMaterialBatches.Add(
                    new ShelfMaterialBatch
                    {
                        MaterialBatchId = batch.Id,
                        WarehouseLocationShelfId = shelf.Id,
                        Quantity = 999_999_999_999_999_999_999_999.99m,
                        UoMId = milliliterUoM.Id,
                        CreatedAt = DateTime.UtcNow,
                    }
                );
            }
        }
        dbContext.SaveChanges();
    }
}
