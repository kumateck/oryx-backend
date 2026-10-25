using APP.Services.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.StockAdjustments;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class StockDiscrepancyWorkflowTests
{
    [Theory]
    [InlineData(7, -3)]
    [InlineData(12, 2)]
    public async Task ApprovedCountPostsSignedLedgerOnce(decimal physical, decimal variance)
    {
        await using var context = NewContext();
        var shelf = Seed(context, quantity: 10);
        var report = new StockDiscrepancy { Id = Guid.NewGuid(),
            ShelfMaterialBatchId = shelf.Id, SystemQuantitySnapshot = 10,
            PhysicalQuantity = physical, Approved = true, Notes = "Physical count verified" };
        context.StockDiscrepancies.Add(report);
        await context.SaveChangesAsync();
        Assert.Single(await context.ShelfMaterialBatches.ToListAsync());

        var first = await StockDiscrepancyService.Reconcile(context, report.Id, Guid.NewGuid());
        Assert.True(first.IsSuccess, first.IsFailure ? first.Error.ToString() : "");
        Assert.Equal(physical, (await context.ShelfMaterialBatches.SingleAsync()).Quantity);
        Assert.Equal(variance, (await context.InventoryLedgers.SingleAsync()).ChangeAmount);
        Assert.Equal(variance > 0 ? variance : 0,
            (await context.BinCardInformation.SingleAsync()).QuantityReceived);
        Assert.False((await StockDiscrepancyService.Reconcile(context, report.Id,
            Guid.NewGuid())).IsSuccess);
        Assert.Single(context.InventoryLedgers);
    }

    [Fact]
    public async Task ChangedBalanceRequiresNewCount()
    {
        await using var context = NewContext();
        var shelf = Seed(context, quantity: 10);
        var report = new StockDiscrepancy { Id = Guid.NewGuid(),
            ShelfMaterialBatchId = shelf.Id, SystemQuantitySnapshot = 10,
            PhysicalQuantity = 8, Approved = true, Notes = "Spillage" };
        context.StockDiscrepancies.Add(report);
        shelf.Quantity = 9;
        await context.SaveChangesAsync();

        var result = await StockDiscrepancyService.Reconcile(context, report.Id, Guid.NewGuid());
        Assert.False(result.IsSuccess);
        Assert.Empty(context.InventoryLedgers);
    }

    private static ApplicationDbContext NewContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());

    private static ShelfMaterialBatch Seed(ApplicationDbContext context, decimal quantity)
    {
        var warehouseId = Guid.NewGuid();
        var warehouse = new Warehouse { Id = warehouseId, Name = "Test warehouse" };
        var location = new WarehouseLocation { Id = Guid.NewGuid(), WarehouseId = warehouseId,
            Warehouse = warehouse };
        var rack = new WarehouseLocationRack { Id = Guid.NewGuid(), WarehouseLocation = location };
        var shelfLocation = new WarehouseLocationShelf { Id = Guid.NewGuid(), WarehouseLocationRack = rack };
        var batch = new MaterialBatch { Id = Guid.NewGuid(), MaterialId = Guid.NewGuid(),
            BatchNumber = "B-1", TotalQuantity = quantity, UoMId = Guid.NewGuid() };
        batch.Material = new Material { Id = batch.MaterialId, Name = "Sample material" };
        batch.UoM = new UnitOfMeasure { Id = batch.UoMId, Symbol = "kg" };
        var shelf = new ShelfMaterialBatch { Id = Guid.NewGuid(), MaterialBatch = batch,
            WarehouseLocationShelf = shelfLocation, Quantity = quantity };
        context.ShelfMaterialBatches.Add(shelf);
        context.SaveChanges();
        return shelf;
    }

    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
