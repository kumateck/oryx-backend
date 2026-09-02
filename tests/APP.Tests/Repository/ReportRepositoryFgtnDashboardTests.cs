using APP.Repository;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Reports.FinishedGoodsTransferNotes;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class FgtnDashboardCurrentUserService(Guid departmentId) : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => departmentId;
    public string DepartmentType => string.Empty;
}

public class ReportRepositoryFgtnDashboardTests
{
    [Fact]
    public async Task Dashboard_groups_inventory_by_uom_and_scopes_dispatches()
    {
        var department = new Department { Id = Guid.NewGuid(), Name = "Production" };
        var otherDepartment = new Department { Id = Guid.NewGuid(), Name = "Other" };
        await using var context = CreateContext(department.Id);
        var uom = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Pieces", Symbol = "pcs" };
        var product = Product("Tablet", department);
        var otherProduct = Product("Other", otherDepartment);
        var packing = new ProductPacking
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Product = product,
            Name = "Bottle",
        };
        var schedule = new ProductionSchedule
        {
            Id = Guid.NewGuid(),
            Code = "PS-001",
            DepartmentId = department.Id,
            Department = department,
        };
        var scheduledProduct = new ProductionScheduleProduct
        {
            Id = Guid.NewGuid(),
            ProductionScheduleId = schedule.Id,
            ProductionSchedule = schedule,
            ProductId = product.Id,
            Product = product,
        };
        var firstBatch = Batch(scheduledProduct, "B-001");
        var secondBatch = Batch(scheduledProduct, "B-002");

        context.AddRange(
            department,
            otherDepartment,
            uom,
            product,
            otherProduct,
            packing,
            schedule,
            scheduledProduct,
            firstBatch,
            secondBatch
        );
        context.FinishedGoodsTransferNotes.AddRange(
            TransferNote(packing, uom, firstBatch, 100, 20),
            TransferNote(packing, uom, secondBatch, 50, 0)
        );
        context.DistributedFinishedProducts.AddRange(
            Dispatch(product, DistributedFinishedProductStatus.Distributed),
            Dispatch(product, DistributedFinishedProductStatus.Arrived),
            Dispatch(otherProduct, DistributedFinishedProductStatus.Arrived)
        );
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetDashboardKpiReport(
            new DashboardFilterDto { DepartmentId = department.Id }
        );

        Assert.True(result.IsSuccess);
        var inventory = Assert.Single(result.Value.InventorySummary);
        Assert.Equal("pcs", inventory.Uom);
        Assert.Equal(130, inventory.AvailableQuantity);
        Assert.Equal(2, inventory.BatchCount);
        Assert.Equal(1, result.Value.DispatchPipeline.AwaitingArrival);
        Assert.Equal(1, result.Value.DispatchPipeline.Arrived);
        Assert.Equal(2, result.Value.DispatchPipeline.Total);
    }

    private static ApplicationDbContext CreateContext(Guid departmentId) =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new FgtnDashboardCurrentUserService(departmentId)
        );

    private static ReportRepository CreateRepository(ApplicationDbContext context) =>
        new(context, null!, null!, NullLogger<ReportRepository>.Instance);

    private static Product Product(string name, Department department) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = name,
            DepartmentId = department.Id,
            Department = department,
            Division = Division.NonBetaLactam,
        };

    private static FinishedGoodsTransferNote TransferNote(
        ProductPacking packing,
        UnitOfMeasure uom,
        BatchManufacturingRecord batch,
        decimal total,
        decimal allocated
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProductPackingId = packing.Id,
            ProductPacking = packing,
            UoMId = uom.Id,
            UoM = uom,
            BatchManufacturingRecordId = batch.Id,
            BatchManufacturingRecord = batch,
            TotalQuantity = total,
            AllocatedQuantity = allocated,
            IsApproved = true,
            CreatedAt = DateTime.UtcNow,
        };

    private static BatchManufacturingRecord Batch(
        ProductionScheduleProduct scheduledProduct,
        string number
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProductionScheduleProductId = scheduledProduct.Id,
            ProductionScheduleProduct = scheduledProduct,
            ProductionActivityStepId = Guid.NewGuid(),
            BatchNumber = number,
            Status = BatchManufacturingStatus.Approved,
        };

    private static DistributedFinishedProduct Dispatch(
        Product product,
        DistributedFinishedProductStatus status
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Product = product,
            Quantity = 10,
            Status = status,
            CreatedAt = DateTime.UtcNow,
        };
}
