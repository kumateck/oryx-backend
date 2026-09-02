using APP.Repository;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Reports.WarehouseDashboardKpi;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class ExpiryCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class ReportRepositoryExpiryRiskTests
{
    [Fact]
    public async Task Expiry_risk_excludes_unlimited_stock_and_uses_the_shelf_uom()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(
            options,
            new ExpiryCurrentUserService()
        );
        var seeded = Seed(context);
        await context.SaveChangesAsync();

        var repository = new ReportRepository(
            context,
            null!,
            null!,
            NullLogger<ReportRepository>.Instance
        );
        var result = await repository.GetExpiryRiskIndex(
            new WarehouseKpiFilterDto(),
            seeded.DepartmentId
        );

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value);
        Assert.Equal("pcs", item.Uom);
        Assert.Equal("25", item.TotalQuantity);
        Assert.Equal(1, item.BatchCount);
    }

    private static SeededExpiryData Seed(ApplicationDbContext context)
    {
        var department = new Department { Id = Guid.NewGuid(), Name = "Warehouse" };
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = "Raw Store",
            Type = WarehouseType.RawMaterialStorage,
            DepartmentId = department.Id,
            Department = department,
        };
        var location = new WarehouseLocation
        {
            Id = Guid.NewGuid(),
            Name = "Main",
            WarehouseId = warehouse.Id,
            Warehouse = warehouse,
        };
        var rack = new WarehouseLocationRack
        {
            Id = Guid.NewGuid(),
            Name = "R1",
            WarehouseLocationId = location.Id,
            WarehouseLocation = location,
        };
        var shelf = new WarehouseLocationShelf
        {
            Id = Guid.NewGuid(),
            Name = "S1",
            Code = "S1",
            WarehouseLocationRackId = rack.Id,
            WarehouseLocationRack = rack,
        };
        var milligram = Uom("Milligram", "mg");
        var pieces = Uom("Pieces", "pcs");
        var finite = Material("PM-1", false);
        var unlimited = Material("RMP020", true);
        var finiteBatch = Batch(finite, milligram, "PACK-1");
        var waterBatch = Batch(unlimited, milligram, "WATER-UNLIMITED");

        context.Departments.Add(department);
        context.Warehouses.Add(warehouse);
        context.WarehouseLocationShelves.Add(shelf);
        context.UnitOfMeasures.AddRange(milligram, pieces);
        context.Materials.AddRange(finite, unlimited);
        context.MaterialBatches.AddRange(finiteBatch, waterBatch);
        context.ShelfMaterialBatches.AddRange(
            ShelfBatch(shelf, finiteBatch, pieces, 25m),
            ShelfBatch(shelf, waterBatch, milligram, decimal.MaxValue / 10m)
        );

        return new SeededExpiryData(department.Id);
    }

    private static UnitOfMeasure Uom(string name, string symbol) =>
        new() { Id = Guid.NewGuid(), Name = name, Symbol = symbol };

    private static Material Material(string code, bool unlimited) =>
        new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = code,
            Kind = MaterialKind.Raw,
            IsUnlimited = unlimited,
        };

    private static MaterialBatch Batch(
        Material material,
        UnitOfMeasure uom,
        string number
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            MaterialId = material.Id,
            Material = material,
            BatchNumber = number,
            TotalQuantity = 25m,
            UoMId = uom.Id,
            UoM = uom,
            ExpiryDate = DateTime.UtcNow.AddDays(120),
        };

    private static ShelfMaterialBatch ShelfBatch(
        WarehouseLocationShelf shelf,
        MaterialBatch batch,
        UnitOfMeasure uom,
        decimal quantity
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            WarehouseLocationShelfId = shelf.Id,
            WarehouseLocationShelf = shelf,
            MaterialBatchId = batch.Id,
            MaterialBatch = batch,
            UoMId = uom.Id,
            UoM = uom,
            Quantity = quantity,
        };

    private sealed record SeededExpiryData(Guid DepartmentId);
}
