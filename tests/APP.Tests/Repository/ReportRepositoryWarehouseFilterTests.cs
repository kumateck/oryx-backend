using APP.Repository;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Reports.WarehouseDashboardKpi;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class WarehouseFilterCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class ReportRepositoryWarehouseFilterTests
{
    [Fact]
    public async Task Warehouse_filters_isolate_capacity_and_freshness_sources()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(
            options,
            new WarehouseFilterCurrentUserService()
        );
        var department = new Department { Id = Guid.NewGuid(), Name = "Stores" };
        var rawChangedAt = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);
        context.Departments.Add(department);
        context.Warehouses.AddRange(
            Warehouse("Raw Store", WarehouseType.RawMaterialStorage,
                Division.BetaLactam, department, rawChangedAt),
            Warehouse("Finished Store", WarehouseType.FinishedGoodsStorage,
                Division.NonBetaLactam, department, rawChangedAt.AddHours(2))
        );
        await context.SaveChangesAsync();

        var repository = new ReportRepository(
            context,
            null!,
            null!,
            NullLogger<ReportRepository>.Instance
        );
        var filter = new WarehouseKpiFilterDto
        {
            WarehouseType = WarehouseType.RawMaterialStorage,
            Division = Division.BetaLactam
        };

        var capacity = await repository.GetWarehouseCapacityUtilisation(
            filter,
            department.Id
        );
        var freshness = await repository.GetWarehouseKpiFreshness(
            filter,
            department.Id
        );

        Assert.True(capacity.IsSuccess);
        Assert.Equal("Raw Store", Assert.Single(capacity.Value).Warehouse);
        Assert.True(freshness.IsSuccess);
        var warehouseSource = Assert.Single(
            freshness.Value,
            item => item.Source == "Warehouse Master"
        );
        Assert.Equal(1, warehouseSource.RecordCount);
        Assert.Equal(rawChangedAt, warehouseSource.LastChangedAt);
    }


    [Fact]
    public async Task Material_batch_details_use_identity_even_when_material_codes_are_equal()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,new WarehouseFilterCurrentUserService());
        var unit = new UnitOfMeasure {Id=Guid.NewGuid(),Name="Kilogram",Symbol="kg"};
        var shelf = new WarehouseLocationShelf {Id=Guid.NewGuid(),Name="Shelf",WarehouseLocationRack=new WarehouseLocationRack {Id=Guid.NewGuid(),Name="Rack",WarehouseLocation=new WarehouseLocation {Id=Guid.NewGuid(),Name="Location",Warehouse=new Warehouse {Id=Guid.NewGuid(),Name="Raw",Type=WarehouseType.RawMaterialStorage}}}};
        var selected = new Material {Id=Guid.NewGuid(),Name="Selected",Code="SAME"};
        var other = new Material {Id=Guid.NewGuid(),Name="Other",Code="SAME"};
        context.ShelfMaterialBatches.AddRange(
            new ShelfMaterialBatch {Id=Guid.NewGuid(),WarehouseLocationShelf=shelf,Quantity=10,UoM=unit,MaterialBatch=new MaterialBatch {Id=Guid.NewGuid(),Material=selected,MaterialId=selected.Id,UoM=unit,BatchNumber="Selected batch"}},
            new ShelfMaterialBatch {Id=Guid.NewGuid(),WarehouseLocationShelf=shelf,Quantity=20,UoM=unit,MaterialBatch=new MaterialBatch {Id=Guid.NewGuid(),Material=other,MaterialId=other.Id,UoM=unit,BatchNumber="Other batch"}});
        await context.SaveChangesAsync();
        var reports = new ReportRepository(context,null!,null!,NullLogger<ReportRepository>.Instance);
        var result = await reports.GetMaterialsStockBatchDetail(materialId:selected.Id);
        var row = Assert.Single(result.Value);
        Assert.Equal("Selected batch",row.BatchNo); Assert.Equal(10,row.QuantityAvailable);
    }

    private static Warehouse Warehouse(
        string name,
        WarehouseType type,
        Division division,
        Department department,
        DateTime changedAt
    ) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Type = type,
        Division = division,
        DepartmentId = department.Id,
        Department = department,
        CreatedAt = changedAt.AddDays(-1),
        UpdatedAt = changedAt
    };
}
