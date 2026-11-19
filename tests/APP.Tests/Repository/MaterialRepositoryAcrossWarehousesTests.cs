using DOMAIN.Entities.Base;
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
    // Shelf and lot share one unit, as stock is recorded today (see StoreStock).
    private static UnitOfMeasure Kg(ApplicationDbContext context) =>
        context.UnitOfMeasures.Local.FirstOrDefault(u => u.Symbol == "kg")
        ?? context.UnitOfMeasures.Add(new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Kilogram", Symbol = "kg" }).Entity;

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
            UoMId = Kg(context).Id,
            Status = BatchStatus.Available,
            ExpiryDate = DateTime.UtcNow.AddDays(-10),
        };
        var usableBatch = new MaterialBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = materialId,
            BatchNumber = $"B-{Guid.NewGuid()}",
            DateReceived = DateTime.UtcNow,
            UoMId = Kg(context).Id,
            Status = BatchStatus.Available,
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
                UoMId = Kg(context).Id,
                Quantity = 200m,
            }
        );
        context.ShelfMaterialBatches.Add(
            new ShelfMaterialBatch
            {
                Id = Guid.NewGuid(),
                WarehouseLocationShelfId = shelf.Id,
                MaterialBatchId = usableBatch.Id,
                UoMId = Kg(context).Id,
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

    /// <summary>Ethanol 96% (0.81 g/mL): Syrup counts it in ml, Ointment in mg. Reported as Syrup's 38,700 L reading 38.7 kg.</summary>
    [Fact]
    public async Task Every_store_is_shown_in_the_requested_unit_with_the_materials_density()
    {
        await using var context = CreateContext();
        var mapper = CreateMapper();
        var material = new DOMAIN.Entities.Materials.Material { Id = Guid.NewGuid(), Code = "RME005", Name = "Ethanol 96%", DensityGramsPerMilliliter = 0.81m };
        var ml = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Millilitre", Symbol = "ml" };
        var mg = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Milligram", Symbol = "mg" };
        context.AddRange(material, ml, mg);
        WarehouseStockDto Row(List<WarehouseStockDto> rows, Guid id) => Assert.Single(rows, row => row.Warehouse.Id == id);
        Guid Store(string name, UnitOfMeasure unit, decimal quantity)
        {
            var department = new DOMAIN.Entities.Departments.Department { Id = Guid.NewGuid(), Name = name };
            var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = name + " Raw Warehouse", Type = WarehouseType.RawMaterialStorage, DepartmentId = department.Id };
            var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), Code = "S1", Name = "Shelf", WarehouseLocationRack = new WarehouseLocationRack
                { Id = Guid.NewGuid(), Name = "Rack", WarehouseLocation = new WarehouseLocation { Id = Guid.NewGuid(), Name = "Room", WarehouseId = warehouse.Id } } };
            var lot = new MaterialBatch { Id = Guid.NewGuid(), MaterialId = material.Id, BatchNumber = name + "/LOT", UoMId = unit.Id, Status = BatchStatus.Available, ExpiryDate = DateTime.UtcNow.AddYears(1) };
            context.AddRange(department, warehouse, shelf, lot);
            context.MaterialDepartments.Add(new DOMAIN.Entities.Materials.MaterialDepartment { Id = Guid.NewGuid(), MaterialId = material.Id, DepartmentId = department.Id, UoMId = unit.Id });
            context.ShelfMaterialBatches.Add(new ShelfMaterialBatch { Id = Guid.NewGuid(), WarehouseLocationShelfId = shelf.Id, MaterialBatchId = lot.Id, UoMId = unit.Id, Quantity = quantity });
            return warehouse.Id;
        }
        var syrup = Store("Syrup", ml, 38_700_000m);   // 38,700 L
        var ointment = Store("Ointment", mg, 471_700_000m); // 471.7 kg
        await context.SaveChangesAsync();
        var repository = new MaterialRepository(context, mapper);

        var inMilligrams = (await repository.GetMaterialStockAcrossWarehouses(material.Id, mg.Id)).Value;
        Assert.Equal(38_700_000m * 0.81m * 1_000m, Row(inMilligrams, syrup).StockQuantity); // 31,347 kg, not 38.7 kg
        Assert.Equal(471_700_000m, Row(inMilligrams, ointment).FreeQuantity);
        Assert.All(inMilligrams, row => Assert.Equal("mg", row.UoM.Symbol));

        var inMillilitres = (await repository.GetMaterialStockAcrossWarehouses(material.Id, ml.Id)).Value;
        Assert.Equal(38_700_000m, Row(inMillilitres, syrup).FreeQuantity);
        Assert.Equal("ml", Row(inMillilitres, ointment).UoM.Symbol);

        // Without a requested unit each store keeps, and names, its own unit.
        var own = (await repository.GetMaterialStockAcrossWarehouses(material.Id)).Value;
        Assert.Equal(("ml", 38_700_000m), (Row(own, syrup).UoM.Symbol, Row(own, syrup).StockQuantity));
        Assert.Equal("mg", Row(own, ointment).UoM.Symbol);
    }
}
