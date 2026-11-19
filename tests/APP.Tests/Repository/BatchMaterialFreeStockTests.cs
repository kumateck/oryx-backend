using APP.Repository;
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

    [Fact]
    public async Task A_batch_applied_before_its_materials_density_was_verified_uses_the_verified_density()
    {
        // L045K004: Propylene Glycol counted in ml with no stored density; the store's lot and other batches' holds are in mg.
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var db = fixture.Harness.Db;
        var millilitre = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Millilitre", Symbol = "ml" };
        db.UnitOfMeasures.Add(millilitre);
        fixture.Lot.Material.DensityGramsPerMilliliter = 1.036m;
        var configuration = fixture.Configuration();
        configuration.RequirementsJson = ProductionMaterialSources.Json(new[] { fixture.Requirement() with
            { Unit = "ml", UnitId = millilitre.Id, BaseUnit = "ml", BaseUnitId = millilitre.Id, Quantity = 100, DensityGramsPerMilliliter = 0 } });
        db.ProductionMaterialConfigurations.Add(configuration);
        var other = OtherBatch(fixture, cancelled: false);
        db.ProductionScheduleProducts.Add(other);
        db.MaterialBatchReservedQuantities.Add(Hold(fixture, other, 518_000)); // 518,000 mg = 500 ml at 1.036 g/ml
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await fixture.Repository.CheckMaterialStockLevelsForProductionSchedule(fixture.Batch.Id, null);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        var row = Assert.Single(result.Value);
        Assert.Equal(1_000m / 1.036m, row.QuantityOnHand, 3); // 1,000,000 mg (1,000 g) on the shelf = 965.25 ml
        Assert.Equal(500, row.TotalFrozenQuantity, 3);
    }
}
