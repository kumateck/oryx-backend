using APP.Repository;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class MoveCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class MaterialRepositoryMoveTests
{
    private static ApplicationDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new MoveCurrentUserService()
        );

    [Fact]
    public async Task Partial_move_to_an_existing_target_preserves_total_stock()
    {
        await using var context = CreateContext();
        var seeded = SeedMovement(context, sourceQuantity: 100m, targetQuantity: 10m);
        await context.SaveChangesAsync();

        var result = await new MaterialRepository(context, null!).MoveMaterialBatchV2(
            new MoveShelfMaterialBatchRequest
            {
                ShelfMaterialBatchId = seeded.SourceBatchId,
                MovedShelfBatchMaterials =
                [
                    new MovedShelfBatchMaterial
                    {
                        WarehouseLocationShelfId = seeded.TargetShelfId,
                        Quantity = 20m,
                        UomId = seeded.UomId,
                    },
                ],
            },
            Guid.NewGuid()
        );

        Assert.True(result.IsSuccess);
        var source = await context.ShelfMaterialBatches.FindAsync(seeded.SourceBatchId);
        var target = await context.ShelfMaterialBatches.FindAsync(seeded.TargetBatchId);
        Assert.Equal(80m, source!.Quantity);
        Assert.Equal(30m, target!.Quantity);
        Assert.Equal(110m, source.Quantity + target.Quantity);
    }

    [Fact]
    public async Task Move_rejects_a_unit_that_differs_from_the_source_batch()
    {
        await using var context = CreateContext();
        var seeded = SeedMovement(context, sourceQuantity: 100m, targetQuantity: 0m);
        await context.SaveChangesAsync();

        var result = await new MaterialRepository(context, null!).MoveMaterialBatchV2(
            new MoveShelfMaterialBatchRequest
            {
                ShelfMaterialBatchId = seeded.SourceBatchId,
                MovedShelfBatchMaterials =
                [
                    new MovedShelfBatchMaterial
                    {
                        WarehouseLocationShelfId = seeded.TargetShelfId,
                        Quantity = 20m,
                        UomId = Guid.NewGuid(),
                    },
                ],
            },
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
        Assert.Equal("MaterialBatch.Move.UomMismatch", Assert.Single(result.Errors).Code);
    }

    private static SeededMovement SeedMovement(
        ApplicationDbContext context,
        decimal sourceQuantity,
        decimal targetQuantity
    )
    {
        var uom = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Millilitre", Symbol = "ml" };
        var material = new Material
        {
            Id = Guid.NewGuid(),
            Name = "Test Material",
            Kind = MaterialKind.Raw,
        };
        var batch = new MaterialBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = material.Id,
            Material = material,
            BatchNumber = "MOVE-1",
            TotalQuantity = sourceQuantity + targetQuantity,
            UoMId = uom.Id,
        };
        var sourceShelf = Shelf("Source");
        var targetShelf = Shelf("Target");
        var source = ShelfBatch(sourceShelf, batch, sourceQuantity, uom.Id);
        var target = ShelfBatch(targetShelf, batch, targetQuantity, uom.Id);

        context.UnitOfMeasures.Add(uom);
        context.Materials.Add(material);
        context.MaterialBatches.Add(batch);
        context.WarehouseLocationShelves.AddRange(sourceShelf, targetShelf);
        context.ShelfMaterialBatches.Add(source);
        if (targetQuantity > 0)
            context.ShelfMaterialBatches.Add(target);

        return new SeededMovement(
            source.Id,
            target.Id,
            targetShelf.Id,
            uom.Id
        );
    }

    private static WarehouseLocationShelf Shelf(string name)
    {
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = $"{name} Warehouse",
            Type = WarehouseType.RawMaterialStorage,
        };
        var location = new WarehouseLocation
        {
            Id = Guid.NewGuid(),
            Name = $"{name} Location",
            WarehouseId = warehouse.Id,
            Warehouse = warehouse,
        };
        var rack = new WarehouseLocationRack
        {
            Id = Guid.NewGuid(),
            Name = $"{name} Rack",
            WarehouseLocationId = location.Id,
            WarehouseLocation = location,
        };
        return new WarehouseLocationShelf
        {
            Id = Guid.NewGuid(),
            Name = $"{name} Shelf",
            Code = name,
            WarehouseLocationRackId = rack.Id,
            WarehouseLocationRack = rack,
        };
    }

    private static ShelfMaterialBatch ShelfBatch(
        WarehouseLocationShelf shelf,
        MaterialBatch batch,
        decimal quantity,
        Guid uomId
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            WarehouseLocationShelfId = shelf.Id,
            WarehouseLocationShelf = shelf,
            MaterialBatchId = batch.Id,
            MaterialBatch = batch,
            Quantity = quantity,
            UoMId = uomId,
        };

    private sealed record SeededMovement(
        Guid SourceBatchId,
        Guid TargetBatchId,
        Guid TargetShelfId,
        Guid UomId
    );
}
