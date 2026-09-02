using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Users;
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

public class RequisitionAlternativeBatchesTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, new FakeCurrentUserService());
    }

    private static IMapper CreateMapper(ApplicationDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton(context);
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static (Guid warehouseId, Guid shelfId) SeedOtherDepartmentWarehouseShelf(
        ApplicationDbContext context
    )
    {
        var otherDepartment = new Department { Id = Guid.NewGuid(), Name = "Other Department" };
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = "Other Raw Warehouse",
            Type = WarehouseType.RawMaterialStorage,
            DepartmentId = otherDepartment.Id,
        };
        var location = new WarehouseLocation
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouse.Id,
            Name = "General",
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

        context.Departments.Add(otherDepartment);
        context.Warehouses.Add(warehouse);
        context.WarehouseLocations.Add(location);
        context.WarehouseLocationRacks.Add(rack);
        context.WarehouseLocationShelves.Add(shelf);

        return (warehouse.Id, shelf.Id);
    }

    private static void SeedShelfBatch(
        ApplicationDbContext context,
        Guid shelfId,
        Guid materialId,
        decimal quantity,
        DateTime? expiryDate
    )
    {
        var uom = new UnitOfMeasure
        {
            Id = Guid.NewGuid(),
            Name = "Kilogram",
            Symbol = "kg",
        };
        context.UnitOfMeasures.Add(uom);

        var batch = new MaterialBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = materialId,
            BatchNumber = $"B-{Guid.NewGuid()}",
            DateReceived = DateTime.UtcNow,
            ExpiryDate = expiryDate,
            Status = BatchStatus.Available,
            UoMId = uom.Id,
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

    private static (Requisition requisition, Department department, Guid userId) SeedStockRequisition(
        ApplicationDbContext context,
        Guid materialId
    )
    {
        var department = new Department { Id = Guid.NewGuid(), Name = "Requesting Department" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            DepartmentId = department.Id,
            Department = department,
            UserName = "requester@example.com",
        };
        var material = new Material
        {
            Id = materialId,
            Code = "RM-TEST",
            Name = "Test Material",
            Kind = MaterialKind.Raw,
        };
        var requisition = new Requisition
        {
            Id = Guid.NewGuid(),
            RequestedById = user.Id,
            DepartmentId = department.Id,
            RequisitionType = RequisitionType.Stock,
        };
        var item = new RequisitionItem
        {
            Id = Guid.NewGuid(),
            RequisitionId = requisition.Id,
            MaterialId = materialId,
            Material = material,
            Quantity = 100m,
        };

        context.Departments.Add(department);
        context.Users.Add(user);
        context.Materials.Add(material);
        context.Requisitions.Add(requisition);
        context.RequisitionItems.Add(item);

        return (requisition, department, user.Id);
    }

    [Fact]
    public async Task ExpiredBatch_NeverSuggestedAsAlternative()
    {
        await using var context = CreateContext();
        var materialId = Guid.NewGuid();
        var (requisition, _, userId) = SeedStockRequisition(context, materialId);
        var (_, shelfId) = SeedOtherDepartmentWarehouseShelf(context);

        // Already expired -- must never be suggested.
        SeedShelfBatch(context, shelfId, materialId, 50m, DateTime.UtcNow.AddDays(-5));
        // About to expire but still usable -- should be suggested.
        SeedShelfBatch(context, shelfId, materialId, 30m, DateTime.UtcNow.AddDays(10));
        await context.SaveChangesAsync();

        var mapper = CreateMapper(context);
        var repository = new RequisitionRepository(
            context,
            mapper,
            null!,
            null!,
            null!,
            null!,
            new MaterialRepository(context, mapper),
            null!,
            null!
        );

        var result = await repository.GetAlternativeBatchesForStockRequisition(
            requisition.Id,
            userId
        );

        Assert.True(result.IsSuccess);
        var materialAlternative = Assert.Single(result.Value);
        var alternative = Assert.Single(materialAlternative.AlternativeBatches);
        Assert.Equal(30m, alternative.QuantityAvailable);
    }

    [Fact]
    public async Task ReservedBatchWithSentinelExpiry_DoesNotBlockAlternatives()
    {
        // Regression test: if the currently-reserved batch's ExpiryDate is the
        // DateTime.MinValue sentinel (no real expiry ever entered), it must
        // not be treated as "the earliest expiry" -- otherwise the alternative
        // search's upper-bound filter becomes impossible to satisfy and
        // silently returns zero alternatives for a perfectly good material.
        await using var context = CreateContext();
        var materialId = Guid.NewGuid();
        var (requisition, requesterDepartment, userId) = SeedStockRequisition(context, materialId);

        // Attach the requisition to a production schedule product + production warehouse,
        // and reserve a batch with a sentinel (unset) expiry date against it.
        var productionWarehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = "Production Warehouse",
            Type = WarehouseType.Production,
            DepartmentId = requesterDepartment.Id,
        };
        context.Warehouses.Add(productionWarehouse);

        var productionScheduleProductId = Guid.NewGuid();
        requisition.ProductionScheduleProductId = productionScheduleProductId;

        var reservedUom = new UnitOfMeasure
        {
            Id = Guid.NewGuid(),
            Name = "Kilogram",
            Symbol = "kg",
        };
        context.UnitOfMeasures.Add(reservedUom);

        var reservedBatch = new MaterialBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = materialId,
            BatchNumber = "RESERVED-1",
            DateReceived = DateTime.UtcNow,
            ExpiryDate = DateTime.MinValue,
            Status = BatchStatus.Available,
            UoMId = reservedUom.Id,
        };
        context.MaterialBatches.Add(reservedBatch);
        context.MaterialBatchReservedQuantities.Add(
            new MaterialBatchReservedQuantity
            {
                Id = Guid.NewGuid(),
                MaterialBatchId = reservedBatch.Id,
                WarehouseId = productionWarehouse.Id,
                ProductionScheduleProductId = productionScheduleProductId,
                UoMId = reservedUom.Id,
                Quantity = 20m,
            }
        );

        var (_, shelfId) = SeedOtherDepartmentWarehouseShelf(context);
        SeedShelfBatch(context, shelfId, materialId, 30m, DateTime.UtcNow.AddDays(10));
        await context.SaveChangesAsync();

        var mapper = CreateMapper(context);
        var repository = new RequisitionRepository(
            context,
            mapper,
            null!,
            null!,
            null!,
            null!,
            new MaterialRepository(context, mapper),
            null!,
            null!
        );

        var result = await repository.GetAlternativeBatchesForStockRequisition(
            requisition.Id,
            userId
        );

        Assert.True(result.IsSuccess);
        var materialAlternative = Assert.Single(result.Value);
        Assert.NotEmpty(materialAlternative.AlternativeBatches);
    }

    [Fact]
    public async Task BatchWithPendingSwap_NotSuggestedAsAlternative()
    {
        // A shelf batch already committed to a pending swap isn't actually free
        // stock - it shouldn't be offered again as an alternative for another
        // swap until that swap is approved or rejected.
        await using var context = CreateContext();
        var materialId = Guid.NewGuid();
        var (requisition, _, userId) = SeedStockRequisition(context, materialId);
        var (otherWarehouseId, shelfId) = SeedOtherDepartmentWarehouseShelf(context);

        SeedShelfBatch(context, shelfId, materialId, 50m, DateTime.UtcNow.AddDays(10));
        var pendingSwapBatch = context.ShelfMaterialBatches.Local.Last();

        SeedShelfBatch(context, shelfId, materialId, 30m, DateTime.UtcNow.AddDays(20));
        var freeBatch = context.ShelfMaterialBatches.Local.Last();

        var swapUom = new UnitOfMeasure
        {
            Id = Guid.NewGuid(),
            Name = "Kilogram",
            Symbol = "kg",
        };
        context.UnitOfMeasures.Add(swapUom);

        context.SwapRequests.Add(
            new SwapRequest
            {
                Id = Guid.NewGuid(),
                FirstWarehouseId = otherWarehouseId,
                SecondWarehouseId = Guid.NewGuid(),
                Status = SwapRequestStatus.Pending,
                FirstSwapShelfMaterialBatches =
                [
                    new SwapShelfMaterialBatch
                    {
                        ShelfMaterialBatchId = pendingSwapBatch.Id,
                        MaterialBatchId = pendingSwapBatch.MaterialBatchId,
                        UoMId = swapUom.Id,
                        Quantity = 50m,
                    },
                ],
                SecondSwapShelfMaterialBatches =
                [
                    new SwapShelfMaterialBatch
                    {
                        ShelfMaterialBatchId = Guid.NewGuid(),
                        MaterialBatchId = Guid.NewGuid(),
                        UoMId = swapUom.Id,
                        Quantity = 50m,
                    },
                ],
            }
        );
        await context.SaveChangesAsync();

        var mapper = CreateMapper(context);
        var repository = new RequisitionRepository(
            context,
            mapper,
            null!,
            null!,
            null!,
            null!,
            new MaterialRepository(context, mapper),
            null!,
            null!
        );

        var result = await repository.GetAlternativeBatchesForStockRequisition(
            requisition.Id,
            userId
        );

        Assert.True(result.IsSuccess);
        var materialAlternative = Assert.Single(result.Value);
        var alternative = Assert.Single(materialAlternative.AlternativeBatches);
        Assert.Equal(freeBatch.MaterialBatchId, alternative.Batch.Id);
        Assert.Equal(30m, alternative.QuantityAvailable);
    }
}
