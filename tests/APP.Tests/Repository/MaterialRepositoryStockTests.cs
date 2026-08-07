using APP.Repository;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class FakeCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class MaterialRepositoryStockTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, new FakeCurrentUserService());
    }

    private static (Guid warehouseId, Guid shelfId) SeedWarehouseShelf(ApplicationDbContext context)
    {
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = "Test Warehouse",
            Type = WarehouseType.RawMaterialStorage,
        };
        var location = new WarehouseLocation
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouse.Id,
            Name = "Test Location",
        };
        var rack = new WarehouseLocationRack
        {
            Id = Guid.NewGuid(),
            WarehouseLocationId = location.Id,
            Name = "Test Rack",
        };
        var shelf = new WarehouseLocationShelf
        {
            Id = Guid.NewGuid(),
            WarehouseLocationRackId = rack.Id,
            Code = "S1",
            Name = "Test Shelf",
        };

        context.Warehouses.Add(warehouse);
        context.WarehouseLocations.Add(location);
        context.WarehouseLocationRacks.Add(rack);
        context.WarehouseLocationShelves.Add(shelf);

        return (warehouse.Id, shelf.Id);
    }

    private static void SeedShelfMaterialBatch(
        ApplicationDbContext context,
        Guid shelfId,
        Guid materialId,
        decimal quantity,
        DateTime? expiryDate,
        DateTime? deletedAt = null
    )
    {
        var batch = new MaterialBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = materialId,
            BatchNumber = $"B-{Guid.NewGuid()}",
            DateReceived = DateTime.UtcNow,
            ExpiryDate = expiryDate,
        };
        var shelfBatch = new ShelfMaterialBatch
        {
            Id = Guid.NewGuid(),
            WarehouseLocationShelfId = shelfId,
            MaterialBatchId = batch.Id,
            Quantity = quantity,
            DeletedAt = deletedAt,
        };

        context.MaterialBatches.Add(batch);
        context.ShelfMaterialBatches.Add(shelfBatch);
    }

    [Fact]
    public async Task FullyExpiredStock_CountsTowardWarehouseStockAndExpiredQuantity()
    {
        await using var context = CreateContext();
        var (warehouseId, shelfId) = SeedWarehouseShelf(context);
        var materialId = Guid.NewGuid();

        SeedShelfMaterialBatch(context, shelfId, materialId, 500m, DateTime.UtcNow.AddDays(-10));
        await context.SaveChangesAsync();

        var repository = new MaterialRepository(context, null!);
        var result = await repository.GetShelfMaterialStockAndExpiredQuantityInWarehouse(
            materialId,
            warehouseId
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(500m, result.Value.WarehouseStock);
        Assert.Equal(500m, result.Value.ExpiredQuantity);
    }

    [Fact]
    public async Task PartiallyExpiredStock_SplitsExpiredFromTotal()
    {
        await using var context = CreateContext();
        var (warehouseId, shelfId) = SeedWarehouseShelf(context);
        var materialId = Guid.NewGuid();

        SeedShelfMaterialBatch(context, shelfId, materialId, 200m, DateTime.UtcNow.AddDays(-5));
        SeedShelfMaterialBatch(context, shelfId, materialId, 300m, DateTime.UtcNow.AddDays(30));
        await context.SaveChangesAsync();

        var repository = new MaterialRepository(context, null!);
        var result = await repository.GetShelfMaterialStockAndExpiredQuantityInWarehouse(
            materialId,
            warehouseId
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(500m, result.Value.WarehouseStock);
        Assert.Equal(200m, result.Value.ExpiredQuantity);
    }

    [Fact]
    public async Task FullyUsableStock_ReturnsZeroExpiredQuantity()
    {
        await using var context = CreateContext();
        var (warehouseId, shelfId) = SeedWarehouseShelf(context);
        var materialId = Guid.NewGuid();

        SeedShelfMaterialBatch(context, shelfId, materialId, 400m, DateTime.UtcNow.AddDays(30));
        await context.SaveChangesAsync();

        var repository = new MaterialRepository(context, null!);
        var result = await repository.GetShelfMaterialStockAndExpiredQuantityInWarehouse(
            materialId,
            warehouseId
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(400m, result.Value.WarehouseStock);
        Assert.Equal(0m, result.Value.ExpiredQuantity);
    }

    [Fact]
    public async Task WarehouseStockMinusExpiredQuantity_MatchesProductionUsableQuantity()
    {
        await using var context = CreateContext();
        var (warehouseId, shelfId) = SeedWarehouseShelf(context);
        var materialId = Guid.NewGuid();

        SeedShelfMaterialBatch(context, shelfId, materialId, 200m, DateTime.UtcNow.AddDays(-5));
        SeedShelfMaterialBatch(context, shelfId, materialId, 300m, DateTime.UtcNow.AddDays(30));
        await context.SaveChangesAsync();

        var repository = new MaterialRepository(context, null!);
        var breakdown = await repository.GetShelfMaterialStockAndExpiredQuantityInWarehouse(
            materialId,
            warehouseId
        );

        // Mirrors the production-usable filter in
        // ProductionScheduleRepository.CheckMaterialStockLevelsForProductionSchedule
        // (ExpiryDate >= DateTime.UtcNow). Batches with a null ExpiryDate are
        // excluded from that filter too, so this identity only holds when every
        // seeded batch has a non-null ExpiryDate, as is the case here.
        var usableQuantity = await context
            .ShelfMaterialBatches.IgnoreQueryFilters()
            .Where(s =>
                s.MaterialBatch.MaterialId == materialId
                && s.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId
                    == warehouseId
                && !s.DeletedAt.HasValue
                && s.MaterialBatch.ExpiryDate >= DateTime.UtcNow
            )
            .SumAsync(s => s.Quantity);

        Assert.Equal(usableQuantity, breakdown.Value.WarehouseStock - breakdown.Value.ExpiredQuantity);
    }

    [Fact]
    public async Task SoftDeletedShelfBatch_ExcludedFromBothTotals()
    {
        await using var context = CreateContext();
        var (warehouseId, shelfId) = SeedWarehouseShelf(context);
        var materialId = Guid.NewGuid();

        SeedShelfMaterialBatch(context, shelfId, materialId, 300m, DateTime.UtcNow.AddDays(30));
        SeedShelfMaterialBatch(
            context,
            shelfId,
            materialId,
            999m,
            DateTime.UtcNow.AddDays(-10),
            deletedAt: DateTime.UtcNow
        );
        await context.SaveChangesAsync();

        var repository = new MaterialRepository(context, null!);
        var result = await repository.GetShelfMaterialStockAndExpiredQuantityInWarehouse(
            materialId,
            warehouseId
        );

        Assert.Equal(300m, result.Value.WarehouseStock);
        Assert.Equal(0m, result.Value.ExpiredQuantity);
    }
}
