using APP.Repository;
using DOMAIN.Entities.Materials.Batch;
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

public class MaterialRepositoryUsableStockTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, new FakeCurrentUserService());
    }

    private static MaterialBatch SeedBatchWithInboundMovement(
        ApplicationDbContext context,
        Guid materialId,
        Guid warehouseId,
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
        context.MaterialBatches.Add(batch);

        context.MassMaterialBatchMovements.Add(
            new MassMaterialBatchMovement
            {
                Id = Guid.NewGuid(),
                BatchId = batch.Id,
                ToWarehouseId = warehouseId,
                Quantity = quantity,
                MovedAt = DateTime.UtcNow,
                MovedById = Guid.NewGuid(),
            }
        );

        return batch;
    }

    [Fact]
    public async Task FullyExpiredBatch_ExcludedFromUsableStock()
    {
        await using var context = CreateContext();
        var materialId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        SeedBatchWithInboundMovement(
            context,
            materialId,
            warehouseId,
            500m,
            DateTime.UtcNow.AddDays(-10)
        );
        await context.SaveChangesAsync();

        var repository = new MaterialRepository(context, null!);
        var result = await repository.GetUsableMassMaterialStockInWarehouse(
            materialId,
            warehouseId
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value);
    }

    [Fact]
    public async Task PartiallyExpiredBatches_OnlyNonExpiredCounted()
    {
        await using var context = CreateContext();
        var materialId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        SeedBatchWithInboundMovement(
            context,
            materialId,
            warehouseId,
            200m,
            DateTime.UtcNow.AddDays(-5)
        );
        SeedBatchWithInboundMovement(
            context,
            materialId,
            warehouseId,
            300m,
            DateTime.UtcNow.AddDays(30)
        );
        await context.SaveChangesAsync();

        var repository = new MaterialRepository(context, null!);
        var result = await repository.GetUsableMassMaterialStockInWarehouse(
            materialId,
            warehouseId
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(300m, result.Value);
    }

    [Fact]
    public async Task FullyUsableBatch_MatchesFullQuantity()
    {
        await using var context = CreateContext();
        var materialId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        SeedBatchWithInboundMovement(
            context,
            materialId,
            warehouseId,
            400m,
            DateTime.UtcNow.AddDays(30)
        );
        await context.SaveChangesAsync();

        var repository = new MaterialRepository(context, null!);
        var result = await repository.GetUsableMassMaterialStockInWarehouse(
            materialId,
            warehouseId
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(400m, result.Value);
    }

    [Fact]
    public async Task NonAvailableStatusBatch_ExcludedEvenWhenNotExpired()
    {
        await using var context = CreateContext();
        var materialId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        SeedBatchWithInboundMovement(
            context,
            materialId,
            warehouseId,
            500m,
            DateTime.UtcNow.AddDays(30),
            status: BatchStatus.Quarantine
        );
        await context.SaveChangesAsync();

        var repository = new MaterialRepository(context, null!);
        var result = await repository.GetUsableMassMaterialStockInWarehouse(
            materialId,
            warehouseId
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value);
    }
}
