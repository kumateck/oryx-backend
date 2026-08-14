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

public class MaterialRepositoryUsableShelfStockTests
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
            Type = WarehouseType.PackagedStorage,
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
        BatchStatus status = BatchStatus.Available
    )
    {
        var batch = new MaterialBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = materialId,
            BatchNumber = $"B-{Guid.NewGuid()}",
            DateReceived = DateTime.UtcNow,
            ExpiryDate = expiryDate,
            Status = status,
        };
        var shelfBatch = new ShelfMaterialBatch
        {
            Id = Guid.NewGuid(),
            WarehouseLocationShelfId = shelfId,
            MaterialBatchId = batch.Id,
            Quantity = quantity,
        };

        context.MaterialBatches.Add(batch);
        context.ShelfMaterialBatches.Add(shelfBatch);
    }

    [Fact]
    public async Task ReadsFromShelfStock_NotMassMovementLedger()
    {
        // Regression test: this material's stock was entered directly onto a shelf
        // (e.g. via Excel import / stock adjustment / swap) with no corresponding
        // MassMaterialBatchMovement row -- the endpoint must still see it.
        await using var context = CreateContext();
        var (warehouseId, shelfId) = SeedWarehouseShelf(context);
        var materialId = Guid.NewGuid();

        SeedShelfMaterialBatch(context, shelfId, materialId, 1_965_140m, DateTime.UtcNow.AddDays(30));
        await context.SaveChangesAsync();

        var repository = new MaterialRepository(context, null!);
        var result = await repository.GetUsableShelfMaterialStockInWarehouse(
            materialId,
            warehouseId
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(1_965_140m, result.Value);
    }

    [Fact]
    public async Task ExpiredStock_ExcludedFromUsableTotal()
    {
        await using var context = CreateContext();
        var (warehouseId, shelfId) = SeedWarehouseShelf(context);
        var materialId = Guid.NewGuid();

        SeedShelfMaterialBatch(context, shelfId, materialId, 500m, DateTime.UtcNow.AddDays(-10));
        await context.SaveChangesAsync();

        var repository = new MaterialRepository(context, null!);
        var result = await repository.GetUsableShelfMaterialStockInWarehouse(
            materialId,
            warehouseId
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value);
    }

    [Fact]
    public async Task NonAvailableStatus_ExcludedEvenWhenNotExpired()
    {
        await using var context = CreateContext();
        var (warehouseId, shelfId) = SeedWarehouseShelf(context);
        var materialId = Guid.NewGuid();

        SeedShelfMaterialBatch(
            context,
            shelfId,
            materialId,
            500m,
            DateTime.UtcNow.AddDays(30),
            status: BatchStatus.Quarantine
        );
        await context.SaveChangesAsync();

        var repository = new MaterialRepository(context, null!);
        var result = await repository.GetUsableShelfMaterialStockInWarehouse(
            materialId,
            warehouseId
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value);
    }

    [Fact]
    public async Task ReservedQuantity_SubtractedFromUsableTotal()
    {
        await using var context = CreateContext();
        var (warehouseId, shelfId) = SeedWarehouseShelf(context);
        var materialId = Guid.NewGuid();

        SeedShelfMaterialBatch(context, shelfId, materialId, 1000m, DateTime.UtcNow.AddDays(30));
        await context.SaveChangesAsync();

        var batchId = await context.MaterialBatches
            .IgnoreQueryFilters()
            .Where(b => b.MaterialId == materialId)
            .Select(b => b.Id)
            .FirstAsync();

        context.MaterialBatchReservedQuantities.Add(
            new MaterialBatchReservedQuantity
            {
                Id = Guid.NewGuid(),
                MaterialBatchId = batchId,
                WarehouseId = warehouseId,
                ProductionScheduleProductId = Guid.NewGuid(),
                UoMId = Guid.NewGuid(),
                Quantity = 400m,
            }
        );
        await context.SaveChangesAsync();

        var repository = new MaterialRepository(context, null!);
        var result = await repository.GetUsableShelfMaterialStockInWarehouse(
            materialId,
            warehouseId
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(600m, result.Value);
    }
}
