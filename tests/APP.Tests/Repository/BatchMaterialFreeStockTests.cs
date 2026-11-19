using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products.Production;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>
/// The batch's material table measures free stock by the one stock rule (StoreStock), as the warehouse's approved-materials
/// page does. Reported for PMS013 on Tobcee Forte Syrup L045K004: 1,754 piece free in the warehouse, 0 on the batch.
/// </summary>
public class BatchMaterialFreeStockTests
{
    private static async Task<ScheduleStockFixture> Applied()
    {
        var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        fixture.Harness.Db.ProductionMaterialConfigurations.Add(fixture.Configuration());
        await fixture.Harness.Db.SaveChangesAsync();
        return fixture;
    }

    private static ProductionScheduleProduct OtherBatch(ScheduleStockFixture fixture, bool cancelled) =>
        new() { Id = Guid.NewGuid(), ProductId = fixture.Batch.ProductId, ProductionScheduleId = fixture.Schedule.Id, BatchSize = BatchSize.Full, Cancelled = cancelled };

    private static MaterialBatchReservedQuantity Hold(ScheduleStockFixture fixture, ProductionScheduleProduct batch, decimal quantity, Guid? unitId = null) => new()
    {
        Id = Guid.NewGuid(), MaterialBatchId = fixture.Lot.Id, Quantity = quantity, UoMId = unitId ?? fixture.Lot.UoMId,
        WarehouseId = fixture.Production.Id, WarehouseLocationShelfId = fixture.Shelf.WarehouseLocationShelfId, ProductionScheduleProductId = batch.Id
    };

    [Fact]
    public async Task Free_stock_counts_only_live_holds_like_the_warehouse()
    {
        using var fixture = await Applied();
        var db = fixture.Harness.Db;
        var live = OtherBatch(fixture, cancelled: false);
        var cancelled = OtherBatch(fixture, cancelled: true);
        db.ProductionScheduleProducts.AddRange(live, cancelled);
        db.MaterialBatchReservedQuantities.AddRange(Hold(fixture, live, 400_000), Hold(fixture, cancelled, 700_000));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await fixture.Repository.CheckMaterialStockLevelsForProductionSchedule(fixture.Batch.Id, null);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        var row = Assert.Single(result.Value);
        // 1,000,000 mg on the shelf, 400,000 held for a live batch: 600,000 free. The cancelled batch's hold no longer counts.
        Assert.Equal(1_000_000, row.QuantityOnHand);
        Assert.Equal(400_000, row.TotalFrozenQuantity);
        Assert.Equal(MaterialRequisitionStatus.InHouse, row.Status);
    }

    [Fact]
    public async Task Another_batchs_hold_in_an_unconvertible_unit_does_not_break_the_list()
    {
        using var fixture = await Applied();
        var db = fixture.Harness.Db;
        var litre = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Litre", Symbol = "L" };
        var other = OtherBatch(fixture, cancelled: false);
        db.UnitOfMeasures.Add(litre);
        db.ProductionScheduleProducts.Add(other);
        db.MaterialBatchReservedQuantities.Add(Hold(fixture, other, 5, litre.Id));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await fixture.Repository.CheckMaterialStockLevelsForProductionSchedule(fixture.Batch.Id, null);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        // The unreadable hold keeps its whole shelf, as the allocator would refuse to take from it.
        Assert.Equal(Assert.Single(result.Value).QuantityOnHand, Assert.Single(result.Value).TotalFrozenQuantity);
    }
}
