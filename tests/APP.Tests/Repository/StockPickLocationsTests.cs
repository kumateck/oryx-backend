using APP.Mapper;
using APP.Repository;
using APP.Tests.QcWorksheets;
using AutoMapper;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace APP.Tests.Repository;

public sealed class StockPickLocationsTests
{
    [Fact]
    public async Task Picks_keep_the_exact_shelf_and_rack_when_one_batch_spans_shelves()
    {
        using var harness = new QcWorksheetTestContext();
        var batch = await harness.SeedMaterialBatch("PICK-LOCATION", BatchStatus.Available);
        batch.ExpiryDate = DateTime.UtcNow.AddYears(1);
        batch.TotalQuantity = 10;
        var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Raw store" };
        var area = new WarehouseLocation { Id = Guid.NewGuid(), Warehouse = warehouse, Name = "Ground floor" };
        var rack = new WarehouseLocationRack { Id = Guid.NewGuid(), Name = "Rack A", WarehouseLocation = area };
        var first = new WarehouseLocationShelf { Id = Guid.NewGuid(), Name = "Shelf 1", Code = "S1", WarehouseLocationRack = rack };
        var second = new WarehouseLocationShelf { Id = Guid.NewGuid(), Name = "Shelf 2", Code = "S2", WarehouseLocationRack = rack };
        harness.Db.ShelfMaterialBatches.AddRange(
            new ShelfMaterialBatch { Id = Guid.NewGuid(), MaterialBatch = batch, WarehouseLocationShelf = first, UoMId = batch.UoMId, Quantity = 4 },
            new ShelfMaterialBatch { Id = Guid.NewGuid(), MaterialBatch = batch, WarehouseLocationShelf = second, UoMId = batch.UoMId, Quantity = 6 });
        await harness.Db.SaveChangesAsync();
        harness.Db.ChangeTracker.Clear();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(harness.Db);
        services.AddSingleton<IHttpContextAccessor, NullHttpContextAccessor>();
        services.AddAutoMapper(_ => { }, typeof(OryxMapper));
        using var provider = services.BuildServiceProvider();
        var repository = new MaterialRepository(harness.Db, provider.GetRequiredService<IMapper>());

        var picked = await repository.BatchesToSupplyForGivenQuantity(batch.MaterialId, warehouse.Id, 7);

        Assert.True(picked.IsSuccess);
        Assert.Equal(2, picked.Value.Count);
        Assert.Equal(7, picked.Value.Sum(item => item.QuantityToTake));
        Assert.All(picked.Value, item => {
            Assert.Equal(batch.Id, item.Batch.Id);
            Assert.Equal(item.WarehouseLocationShelfId, item.WarehouseLocationShelf.Id);
            Assert.Equal("Rack A", item.WarehouseLocationShelf.WarehouseLocationRack.Name);
            Assert.Contains(item.WarehouseLocationShelf.Name, new[] { "Shelf 1", "Shelf 2" });
        });
        Assert.Equal(2, picked.Value.Select(item => item.WarehouseLocationShelf.Id).Distinct().Count());
        Assert.Equal(10, await harness.Db.ShelfMaterialBatches.SumAsync(item => item.Quantity));

        var production = Guid.NewGuid();
        Assert.True((await repository.ReserveQuantityFromBatchForProduction(
            batch.Id, warehouse.Id, production, 2, batch.UoMId, second.Id)).IsSuccess);
        harness.Db.ChangeTracker.Clear();
        var reserved = await repository.GetReservedBatchesAndQuantityForProductionWarehouse(
            batch.MaterialId, warehouse.Id, production);
        var allocation = Assert.Single(reserved);
        Assert.Equal(second.Id, allocation.WarehouseLocationShelf.Id);
        Assert.Equal("Shelf 2", allocation.WarehouseLocationShelf.Name);
        Assert.Equal("Rack A", allocation.WarehouseLocationShelf.WarehouseLocationRack.Name);
    }
}
