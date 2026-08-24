using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class FakeCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class MaterialRepositoryAcrossWarehousesTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, new FakeCurrentUserService());
    }

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task GetMaterialStockAcrossWarehouses_ExposesExpiredQuantityPerWarehouse()
    {
        await using var context = CreateContext();
        var mapper = CreateMapper();
        var materialId = Guid.NewGuid();

        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = "Raw Warehouse",
            Type = WarehouseType.RawMaterialStorage,
        };
        var location = new WarehouseLocation
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouse.Id,
            Name = "Location",
        };
        var rack = new WarehouseLocationRack
        {
            Id = Guid.NewGuid(),
            WarehouseLocationId = location.Id,
            Name = "Rack",
        };
        var shelf = new WarehouseLocationShelf
        {
            Id = Guid.NewGuid(),
            WarehouseLocationRackId = rack.Id,
            Code = "S1",
            Name = "Shelf",
        };

        context.Warehouses.Add(warehouse);
        context.WarehouseLocations.Add(location);
        context.WarehouseLocationRacks.Add(rack);
        context.WarehouseLocationShelves.Add(shelf);

        var expiredBatch = new MaterialBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = materialId,
            BatchNumber = $"B-{Guid.NewGuid()}",
            DateReceived = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(-10),
        };
        var usableBatch = new MaterialBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = materialId,
            BatchNumber = $"B-{Guid.NewGuid()}",
            DateReceived = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
        };
        context.MaterialBatches.Add(expiredBatch);
        context.MaterialBatches.Add(usableBatch);

        context.ShelfMaterialBatches.Add(
            new ShelfMaterialBatch
            {
                Id = Guid.NewGuid(),
                WarehouseLocationShelfId = shelf.Id,
                MaterialBatchId = expiredBatch.Id,
                Quantity = 200m,
            }
        );
        context.ShelfMaterialBatches.Add(
            new ShelfMaterialBatch
            {
                Id = Guid.NewGuid(),
                WarehouseLocationShelfId = shelf.Id,
                MaterialBatchId = usableBatch.Id,
                Quantity = 300m,
            }
        );

        await context.SaveChangesAsync();

        var repository = new MaterialRepository(context, mapper);
        var result = await repository.GetMaterialStockAcrossWarehouses(materialId);

        Assert.True(result.IsSuccess);
        var entry = Assert.Single(result.Value, w => w.Warehouse.Id == warehouse.Id);
        Assert.Equal(500m, entry.StockQuantity);
        Assert.Equal(200m, entry.ExpiredQuantity);
    }
}
