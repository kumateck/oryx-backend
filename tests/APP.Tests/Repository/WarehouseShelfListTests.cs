using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class ShelfListCurrentUserService(Guid departmentId) : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => departmentId;
    public string DepartmentType => string.Empty;
}

public class WarehouseShelfListTests
{
    private static ApplicationDbContext CreateContext(Guid departmentId)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, new ShelfListCurrentUserService(departmentId));
    }

    private static IMapper CreateMapper(ApplicationDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(context);
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task ShelfList_LoadsOnlyListRelationships_AndSearchesByCode()
    {
        var departmentId = Guid.NewGuid();
        await using var context = CreateContext(departmentId);
        var department = new Department { Id = departmentId, Name = "Stores" };
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = "Packaging Warehouse",
            Type = WarehouseType.PackagedStorage,
            DepartmentId = department.Id,
        };
        var location = new WarehouseLocation
        {
            Id = Guid.NewGuid(),
            Name = "Foils",
            WarehouseId = warehouse.Id,
        };
        var rack = new WarehouseLocationRack
        {
            Id = Guid.NewGuid(),
            Name = "G/23",
            WarehouseLocationId = location.Id,
        };
        var shelf = new WarehouseLocationShelf
        {
            Id = Guid.NewGuid(),
            Code = "T/PM/G/23/C",
            Name = "G/23/C",
            WarehouseLocationRackId = rack.Id,
        };
        var material = new Material
        {
            Id = Guid.NewGuid(),
            Code = "PMC001",
            Name = "Printed Foil",
            Kind = MaterialKind.Package,
        };
        var batch = new MaterialBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = material.Id,
            BatchNumber = "B-001",
            DateReceived = DateTime.UtcNow,
        };

        context.AddRange(department, warehouse, location, rack, shelf, material, batch);
        context.ShelfMaterialBatches.Add(
            new ShelfMaterialBatch
            {
                Id = Guid.NewGuid(),
                WarehouseLocationShelfId = shelf.Id,
                MaterialBatchId = batch.Id,
                Quantity = 500,
            }
        );
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new WarehouseRepository(
            context,
            CreateMapper(context),
            null!,
            NullLogger<WarehouseRepository>.Instance
        );

        var result = await repository.GetWarehouseLocationShelves(
            page: 1,
            pageSize: 50,
            searchQuery: "T/PM/G/23/C"
        );

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Data);
        Assert.Equal(shelf.Id, item.Id);
        Assert.Equal("G/23", item.WarehouseLocationRack.Name);
        Assert.Equal("Foils", item.WarehouseLocationRack.WarehouseLocation.Name);
        Assert.Equal(
            "Packaging Warehouse",
            item.WarehouseLocationRack.WarehouseLocation.Warehouse.Name
        );
        Assert.Empty(item.MaterialBatches);
        Assert.Empty(context.ChangeTracker.Entries<ShelfMaterialBatch>());
    }
}
