using APP.Repository;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Reports.WarehouseDashboardKpi;
using DOMAIN.Entities.Warehouses;
using DOMAIN.Entities.Requisitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using INFRASTRUCTURE.Context;
using SHARED.Services.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace APP.Tests.Repository;

public class ScheduleMaterialValidityTests
{
    private sealed class FixtureActor(Guid department) : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => department;
        public string DepartmentType => "Production";
    }

    [Theory]
    [InlineData(false, -30, 120, BatchStatus.Available, 1_000_000)]
    [InlineData(true, -30, 120, BatchStatus.Available, 1_000_000)]
    [InlineData(false, -30, -1, BatchStatus.Available, 0)]
    [InlineData(true, -30, -1, BatchStatus.Available, 0)]
    [InlineData(false, 30, -1, BatchStatus.Available, 1_000_000)]
    [InlineData(true, 30, -1, BatchStatus.Available, 1_000_000)]
    [InlineData(false, -30, 120, BatchStatus.Frozen, 0)]
    [InlineData(true, -30, 120, BatchStatus.Frozen, 0)]
    public async Task Prestart_stock_uses_validity_on_both_master_and_applied_configuration_paths(
        bool applied, int expiryDays, int retestDays, BatchStatus status, int quantity)
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        fixture.Lot.ExpiryDate = DateTime.UtcNow.AddDays(expiryDays);
        fixture.Lot.RetestDate = DateTime.UtcNow.AddDays(retestDays);
        fixture.Lot.Status = status;
        if (applied) fixture.Harness.Db.ProductionMaterialConfigurations.Add(fixture.Configuration());
        await fixture.Harness.Db.SaveChangesAsync();
        fixture.Harness.Db.ChangeTracker.Clear();
        var result = await fixture.Repository.CheckMaterialStockLevelsForProductionSchedule(fixture.Batch.Id, null);
        Assert.True(result.IsSuccess, result.Error.Description);
        var row = Assert.Single(result.Value);
        Assert.Equal(quantity, row.QuantityOnHand);
        Assert.Equal(0, row.FrozenQuantity);
        Assert.Equal(quantity > 0 ? MaterialRequisitionStatus.InHouse : MaterialRequisitionStatus.None, row.Status);
        Assert.False(await fixture.Harness.Db.ProductionActivities.AnyAsync());
    }

    [Fact]
    public async Task Density_enabled_BOM_is_converted_to_the_department_stock_unit_for_production()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var db = fixture.Harness.Db;
        var mg = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Milligram", Symbol = "mg" };
        var ml = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Milliliter", Symbol = "ml" };
        db.UnitOfMeasures.AddRange(mg, ml);
        fixture.Lot.UoMId = ml.Id;
        fixture.Lot.UoM = ml;
        fixture.Shelf.UoMId = ml.Id;
        var link = await db.MaterialDepartments.SingleAsync(item => item.MaterialId == fixture.Lot.MaterialId);
        link.UoMId = ml.Id;
        link.UoM = ml;
        link.Density = 1.5m;
        var item = await db.BillOfMaterialItems.SingleAsync();
        item.BaseUoMId = mg.Id;
        item.BaseUoM = mg;
        item.BaseQuantity = 1_000_000;
        item.PrescribedQuantity = 1_000_000;
        item.UseDensityConversion = true;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var source = await ProductionMaterialSources.Load(db, fixture.Batch.Id, MaterialKind.Raw);
        Assert.True(source.IsSuccess, source.Error.Description);
        var requirement = Assert.Single(source.Value.Requirements);
        Assert.Equal(666.66666666666666666666666667m, requirement.Quantity);
        Assert.Equal("ml", requirement.Unit);
        Assert.Equal("mg", requirement.BaseUnit);
        Assert.Equal(1.5m, requirement.DensityGramsPerMilliliter);
        Assert.True(requirement.UseDensityConversion);

        var rows = await fixture.Repository.CheckMaterialStockLevelsForProductionSchedule(fixture.Batch.Id, null);
        Assert.True(rows.IsSuccess, rows.Error.Description);
        Assert.Equal(requirement.Quantity, Assert.Single(rows.Value).QuantityNeeded);
        Assert.Equal("ml", Assert.Single(rows.Value).MaterialDepartment.UoM.Symbol);
    }

    [Theory]
    [InlineData(MaterialKind.Raw, 0)]
    [InlineData(MaterialKind.Package, 0)]
    public async Task Allocations_accept_retest_extended_raw_and_packaging_stock(MaterialKind kind, int shortage)
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        fixture.Lot.Material.Kind = kind;
        await fixture.Harness.Db.SaveChangesAsync();
        fixture.Harness.Db.ChangeTracker.Clear();
        var source = new ProductionMaterialSource(fixture.Batch, fixture.Department.Id, fixture.Storage.Id,
            fixture.Production.Id, [fixture.Requirement()], "test", "{}", []);
        var plan = await ProductionMaterialAllocator.Plan(fixture.Harness.Db, source, []);
        Assert.Empty(plan.Blockers);
        Assert.Equal(shortage, plan.Shortages[fixture.Lot.MaterialId]);
        Assert.Equal(shortage == 0 ? 300_000 : 0, plan.Allocations.Sum(item => item.Quantity));
        var evidence = plan.EvidenceHash;
        var lot = await fixture.Harness.Db.MaterialBatches.SingleAsync();
        lot.RetestDate = DateTime.UtcNow.AddDays(-1);
        await fixture.Harness.Db.SaveChangesAsync();
        var stale = await ProductionMaterialAllocator.Plan(fixture.Harness.Db, source, []);
        Assert.NotEqual(evidence, stale.EvidenceHash);
        Assert.Equal(300_000, stale.Shortages[fixture.Lot.MaterialId]);
    }

    [Fact]
    public async Task Replacement_swaps_honour_retest_without_mutating_stock_or_accepting_expired_extensions()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var db = fixture.Harness.Db;
        var other = new Warehouse { Id = Guid.NewGuid(), Name = "Other raw store", DepartmentId = fixture.OtherDepartment.Id };
        var replacement = new ShelfMaterialBatch { Id = Guid.NewGuid(), MaterialBatchId = fixture.Lot.Id,
            Quantity = 1_000_000, UoMId = fixture.Lot.UoMId, WarehouseLocationShelf = new() { Id = Guid.NewGuid(), WarehouseLocationRack = new()
            { Id = Guid.NewGuid(), WarehouseLocation = new() { Id = Guid.NewGuid(), Warehouse = other } } } };
        db.ShelfMaterialBatches.Add(replacement);
        var requisition = new Requisition { Id = Guid.NewGuid(), DepartmentId = fixture.Department.Id, RequestedById = fixture.Actor.Id,
            ProductionScheduleProductId = fixture.Batch.Id, RequisitionType = RequisitionType.Stock,
            Items = [new() { Id = Guid.NewGuid(), MaterialId = fixture.Lot.MaterialId, UoMId = fixture.Lot.UoMId, Quantity = 300_000 }] };
        db.Requisitions.Add(requisition);
        db.ProductionMaterialConfigurations.Add(fixture.Configuration());
        db.MaterialBatchReservedQuantities.Add(new() { Id = Guid.NewGuid(), MaterialBatchId = fixture.Lot.Id,
            ProductionScheduleProductId = fixture.Batch.Id, WarehouseId = fixture.Production.Id, UoMId = fixture.Lot.UoMId,
            WarehouseLocationShelfId = fixture.Shelf.WarehouseLocationShelfId, Quantity = 300_000 });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        CreateSwapShelfMaterialBatch Selection(ShelfMaterialBatch shelf) => new()
        { ShelfMaterialBatchId = shelf.Id, MaterialBatchId = fixture.Lot.Id, UoMId = fixture.Lot.UoMId, Quantity = 300_000 };
        var request = new CreateSwapRequest { StockRequisitionId = requisition.Id, FirstWarehouseId = fixture.Storage.Id, SecondWarehouseId = other.Id,
            FirstSwapShelfMaterialBatches = [Selection(fixture.Shelf)], SecondSwapShelfMaterialBatches = [Selection(replacement)] };
        var result = await ProductionMaterialSwapRules.Validate(db, request);
        Assert.True(result.IsSuccess, result.Error.Description);
        var lot = await db.MaterialBatches.IgnoreQueryFilters().SingleAsync();
        lot.RetestDate = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();
        result = await ProductionMaterialSwapRules.Validate(db, request);
        Assert.Equal("Swap.Replacement", result.Error.Code);
        Assert.Equal(2_000_000, await db.ShelfMaterialBatches.IgnoreQueryFilters().SumAsync(item => item.Quantity));
    }

    [Fact]
    public async Task A_creators_later_department_switch_does_not_change_the_schedules_stock_store()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        fixture.Creator.DepartmentId = fixture.OtherDepartment.Id;
        await fixture.Harness.Db.SaveChangesAsync();
        fixture.Harness.Db.ChangeTracker.Clear();
        var result = await fixture.Repository.CheckMaterialStockLevelsForProductionSchedule(fixture.Batch.Id, null);
        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(fixture.Storage.Id, Assert.Single(result.Value).StorageWarehouseId);
        Assert.Equal(1_000_000, Assert.Single(result.Value).QuantityOnHand);
    }

    [Fact]
    public async Task Both_risk_reports_use_the_extension_instead_of_original_expiry()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        fixture.Harness.Db.ChangeTracker.Clear();
        var reports = new ReportRepository(fixture.Harness.Db, fixture.Mapper, null!, NullLogger<ReportRepository>.Instance);
        var global = await reports.GetExpiryRiskIndex(new WarehouseKpiFilterDto());
        var branch = await reports.GetExpiryRiskIndex(new WarehouseKpiFilterDto(), fixture.Department.Id);
        Assert.Equal("> 90 days", Assert.Single(global.Value).ExpiryWindow);
        Assert.Equal("> 90 days", Assert.Single(branch.Value).ExpiryWindow);
    }
}
