using APP.Repository;
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
