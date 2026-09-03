using APP.Repository;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.Packing;
using DOMAIN.Entities.Reports;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class ProductionKpiCurrentUserService(Guid departmentId) : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => departmentId;
    public string DepartmentType => string.Empty;
}

public class ReportRepositoryProductionKpiTests
{
    [Fact]
    public async Task Bmr_release_rate_uses_manufacturing_date_and_approved_as_issued()
    {
        var fixture = await CreateFixture();
        await using var context = fixture.Context;
        context.BatchManufacturingRecords.AddRange(
            Batch(fixture.FirstProduct, BatchManufacturingStatus.Approved, new(2026, 8, 5)),
            Batch(fixture.FirstProduct, BatchManufacturingStatus.Testing, new(2026, 8, 10)),
            Batch(fixture.FirstProduct, BatchManufacturingStatus.Rejected, new(2026, 8, 31)),
            Batch(fixture.FirstProduct, BatchManufacturingStatus.Approved, new(2026, 9, 1))
        );
        await context.SaveChangesAsync();

        var result = await Repository(context).GetBmrReleaseRate(August(fixture.Department.Id));

        Assert.True(result.IsSuccess);
        var row = Assert.Single(result.Value);
        Assert.Equal(3, row.TotalBmrs);
        Assert.Equal(1, row.Pending);
        Assert.Equal(1, row.Issued);
        Assert.Equal(1, row.Rejected);
        Assert.Equal(33.33m, row.ReleaseRatePercentage);
    }

    [Fact]
    public async Task Yield_performance_uses_packing_expected_yield_and_actual_packed_quantity()
    {
        var fixture = await CreateFixture();
        await using var context = fixture.Context;
        var first = Packing(fixture.FirstProduct, fixture.Packing, 90, -10);
        var second = Packing(fixture.SecondProduct, fixture.Packing, 100, 0);
        var outsidePeriod = Packing(fixture.SecondProduct, fixture.Packing, 120, 20);
        context.FinalPackings.AddRange(first, second, outsidePeriod);
        await context.SaveChangesAsync();
        first.CreatedAt = new DateTime(2026, 8, 5);
        second.CreatedAt = new DateTime(2026, 8, 31);
        outsidePeriod.CreatedAt = new DateTime(2026, 9, 1);
        await context.SaveChangesAsync();

        var result = await Repository(context).GetYieldPerformance(August(fixture.Department.Id));

        Assert.True(result.IsSuccess);
        var row = Assert.Single(result.Value);
        Assert.Equal(2, row.BatchCount);
        Assert.Equal(200, row.ExpectedYield);
        Assert.Equal(190, row.ActualQuantityPacked);
        Assert.Equal(-10, row.TotalGainOrLoss);
        Assert.Equal(-5, row.VariancePercentage);
    }

    private static ProductionKpiFilter August(Guid departmentId) =>
        new()
        {
            DepartmentId = departmentId,
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 8, 31),
        };

    private static async Task<Fixture> CreateFixture()
    {
        var department = new Department { Id = Guid.NewGuid(), Name = "Production" };
        var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new ProductionKpiCurrentUserService(department.Id)
        );
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Tablet",
            Code = "TAB",
            DepartmentId = department.Id,
            Department = department,
        };
        var packing = new ProductPacking
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Product = product,
            Name = "Bottle",
            ExpectedYield = 100,
        };
        var schedule = new ProductionSchedule
        {
            Id = Guid.NewGuid(),
            Code = "PS-001",
            DepartmentId = department.Id,
            Department = department,
        };
        var first = ScheduledProduct(product, packing, schedule, "B-001");
        var second = ScheduledProduct(product, packing, schedule, "B-002");
        context.AddRange(department, product, packing, schedule, first, second);
        await context.SaveChangesAsync();
        return new Fixture(context, department, packing, first, second);
    }

    private static ProductionScheduleProduct ScheduledProduct(
        Product product,
        ProductPacking packing,
        ProductionSchedule schedule,
        string batch
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Product = product,
            ProductPackingId = packing.Id,
            ProductPacking = packing,
            ProductionScheduleId = schedule.Id,
            ProductionSchedule = schedule,
            BatchNumber = batch,
        };

    private static BatchManufacturingRecord Batch(
        ProductionScheduleProduct product,
        BatchManufacturingStatus status,
        DateTime manufacturingDate
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProductionScheduleProductId = product.Id,
            ProductionScheduleProduct = product,
            ProductionActivityStepId = Guid.NewGuid(),
            BatchNumber = product.BatchNumber,
            ManufacturingDate = manufacturingDate,
            Status = status,
        };

    private static FinalPacking Packing(
        ProductionScheduleProduct product,
        ProductPacking packing,
        decimal actual,
        decimal gainOrLoss
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProductionScheduleProductId = product.Id,
            ProductionScheduleProduct = product,
            ProductPackingId = packing.Id,
            ProductPacking = packing,
            TotalQuantityPacked = actual,
            TotalGainOrLoss = gainOrLoss,
        };

    private static ReportRepository Repository(ApplicationDbContext context) =>
        new(context, null!, null!, NullLogger<ReportRepository>.Instance);

    private sealed record Fixture(
        ApplicationDbContext Context,
        Department Department,
        ProductPacking Packing,
        ProductionScheduleProduct FirstProduct,
        ProductionScheduleProduct SecondProduct
    );
}
