using APP.Repository;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>A refused production start must say which material is short and why, not only that something is.</summary>
public class ProductionStartShortageTests
{
    [Fact]
    public async Task Shortage_names_the_material_and_the_other_batches_holding_its_stock()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var db = fixture.Harness.Db;
        var holder = new ProductionScheduleProduct { Id = Guid.NewGuid(), ProductId = fixture.Batch.ProductId,
            ProductionScheduleId = fixture.Batch.ProductionScheduleId, BatchSize = BatchSize.Full, BatchNumber = "HOLD-77" };
        db.ProductionScheduleProducts.Add(holder);
        db.MaterialBatchReservedQuantities.Add(new() { Id = Guid.NewGuid(), MaterialBatchId = fixture.Lot.Id,
            ProductionScheduleProductId = holder.Id, WarehouseId = fixture.Production.Id, UoMId = fixture.Lot.UoMId,
            WarehouseLocationShelfId = fixture.Shelf.WarehouseLocationShelfId, Quantity = 900_000 });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var requirement = fixture.Requirement();
        var source = new ProductionMaterialSource(fixture.Batch, fixture.Department.Id, fixture.Storage.Id,
            fixture.Production.Id, [requirement], "test", "{}", []);

        var plan = await ProductionMaterialAllocator.Plan(db, source, []);

        Assert.Equal(200_000, plan.Shortages[fixture.Lot.MaterialId]);
        var detail = Assert.Single(plan.ShortageDetails);
        Assert.StartsWith($"{requirement.Name}: needs 300000 {requirement.Unit}, 100000 {requirement.Unit} available to reserve", detail);
        Assert.Contains("short by 200000", detail);
        Assert.Contains($"900000 {requirement.Unit} is held by other batches' unissued reservations (HOLD-77)", detail);
    }

    [Fact]
    public async Task Shortage_reports_stock_past_its_retest_date()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var db = fixture.Harness.Db;
        fixture.Lot.ExpiryDate = DateTime.UtcNow.AddDays(-30);
        fixture.Lot.RetestDate = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var requirement = fixture.Requirement();
        var source = new ProductionMaterialSource(fixture.Batch, fixture.Department.Id, fixture.Storage.Id,
            fixture.Production.Id, [requirement], "test", "{}", []);

        var plan = await ProductionMaterialAllocator.Plan(db, source, []);

        var detail = Assert.Single(plan.ShortageDetails);
        Assert.Contains($"1000000 {requirement.Unit} is expired or past its retest date", detail);
        Assert.DoesNotContain("held by other batches", detail);
    }

    [Fact]
    public async Task Enough_stock_produces_no_shortage_detail()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        fixture.Harness.Db.ChangeTracker.Clear();
        var source = new ProductionMaterialSource(fixture.Batch, fixture.Department.Id, fixture.Storage.Id,
            fixture.Production.Id, [fixture.Requirement()], "test", "{}", []);
        var plan = await ProductionMaterialAllocator.Plan(fixture.Harness.Db, source, []);
        Assert.Empty(plan.ShortageDetails);
        Assert.True(await fixture.Harness.Db.MaterialBatches.AnyAsync());
    }
}
