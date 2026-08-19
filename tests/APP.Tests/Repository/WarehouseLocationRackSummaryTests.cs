using APP.Repository;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class FakeCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class WarehouseLocationRackSummaryTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, new FakeCurrentUserService());
    }

    private static (Guid userId, Guid rackId, Guid shelfId) SeedDepartmentWithPackageWarehouse(
        ApplicationDbContext context
    )
    {
        var department = new Department { Id = Guid.NewGuid(), Name = "Test Department" };
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = "Test Package Warehouse",
            Type = WarehouseType.PackagedStorage,
            DepartmentId = department.Id,
        };
        department.Warehouses.Add(warehouse);

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
            Name = "Rack 1",
        };
        var shelf = new WarehouseLocationShelf
        {
            Id = Guid.NewGuid(),
            WarehouseLocationRackId = rack.Id,
            Code = "S1",
            Name = "Shelf 1",
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            DepartmentId = department.Id,
            Department = department,
            UserName = "test@example.com",
        };

        context.Departments.Add(department);
        context.Warehouses.Add(warehouse);
        context.WarehouseLocations.Add(location);
        context.WarehouseLocationRacks.Add(rack);
        context.WarehouseLocationShelves.Add(shelf);
        context.Users.Add(user);

        return (user.Id, rack.Id, shelf.Id);
    }

    [Fact]
    public async Task ReturnsRackWithShelfMaterialBatchSummary()
    {
        await using var context = CreateContext();
        var (userId, rackId, shelfId) = SeedDepartmentWithPackageWarehouse(context);

        var material = new Material
        {
            Id = Guid.NewGuid(),
            Code = "PMC151",
            Name = "Cartons Paracetamol Tablet",
            Kind = MaterialKind.Package,
        };
        var uom = new UnitOfMeasure
        {
            Id = Guid.NewGuid(),
            Name = "Pieces",
            Symbol = "PCS",
        };
        var batch = new MaterialBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = material.Id,
            BatchNumber = "B-001",
            DateReceived = DateTime.UtcNow,
            UoMId = uom.Id,
            Status = BatchStatus.Available,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
        };
        var shelfBatch = new ShelfMaterialBatch
        {
            Id = Guid.NewGuid(),
            WarehouseLocationShelfId = shelfId,
            MaterialBatchId = batch.Id,
            Quantity = 5000m,
        };

        context.Materials.Add(material);
        context.UnitOfMeasures.Add(uom);
        context.MaterialBatches.Add(batch);
        context.ShelfMaterialBatches.Add(shelfBatch);
        await context.SaveChangesAsync();

        var repository = new WarehouseRepository(
            context,
            null!,
            null!,
            NullLogger<WarehouseRepository>.Instance
        );

        var result = await repository.GetWarehouseLocationRackSummaries(
            MaterialKind.Package,
            userId
        );

        Assert.True(result.IsSuccess);
        var rack = Assert.Single(result.Value);
        Assert.Equal(rackId, rack.Id);
        var shelf = Assert.Single(rack.Shelves);
        Assert.Equal(shelfId, shelf.Id);
        var summary = Assert.Single(shelf.MaterialBatches);
        Assert.Equal("PMC151", summary.MaterialCode);
        Assert.Equal("Cartons Paracetamol Tablet", summary.MaterialName);
        Assert.Equal(5000m, summary.Quantity);
        Assert.Equal("PCS", summary.UoM.Symbol);
        Assert.Equal(BatchStatus.Available, summary.Status);
    }

    [Fact]
    public async Task SoftDeletedShelfBatch_Excluded()
    {
        await using var context = CreateContext();
        var (userId, _, shelfId) = SeedDepartmentWithPackageWarehouse(context);

        var material = new Material
        {
            Id = Guid.NewGuid(),
            Code = "PMC999",
            Name = "Deleted Material",
            Kind = MaterialKind.Package,
        };
        var uom = new UnitOfMeasure
        {
            Id = Guid.NewGuid(),
            Name = "Pieces",
            Symbol = "PCS",
        };
        var batch = new MaterialBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = material.Id,
            BatchNumber = "B-002",
            DateReceived = DateTime.UtcNow,
            UoMId = uom.Id,
        };
        var shelfBatch = new ShelfMaterialBatch
        {
            Id = Guid.NewGuid(),
            WarehouseLocationShelfId = shelfId,
            MaterialBatchId = batch.Id,
            Quantity = 100m,
            DeletedAt = DateTime.UtcNow,
        };

        context.Materials.Add(material);
        context.UnitOfMeasures.Add(uom);
        context.MaterialBatches.Add(batch);
        context.ShelfMaterialBatches.Add(shelfBatch);
        await context.SaveChangesAsync();

        var repository = new WarehouseRepository(
            context,
            null!,
            null!,
            NullLogger<WarehouseRepository>.Instance
        );

        var result = await repository.GetWarehouseLocationRackSummaries(
            MaterialKind.Package,
            userId
        );

        Assert.True(result.IsSuccess);
        var rack = Assert.Single(result.Value);
        var shelf = Assert.Single(rack.Shelves);
        Assert.Empty(shelf.MaterialBatches);
    }

    [Fact]
    public async Task NoPackagingWarehouseForUser_ReturnsError()
    {
        await using var context = CreateContext();
        var department = new Department { Id = Guid.NewGuid(), Name = "No Warehouse Dept" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            DepartmentId = department.Id,
            Department = department,
            UserName = "nowarehouse@example.com",
        };
        context.Departments.Add(department);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var repository = new WarehouseRepository(
            context,
            null!,
            null!,
            NullLogger<WarehouseRepository>.Instance
        );

        var result = await repository.GetWarehouseLocationRackSummaries(
            MaterialKind.Package,
            user.Id
        );

        Assert.True(result.IsFailure);
    }
}
