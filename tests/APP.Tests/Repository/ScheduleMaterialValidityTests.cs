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

    [Theory]
    [InlineData("ml", BatchSize.Full, "1000")]
    [InlineData("ml", BatchSize.Half, "500")]
    [InlineData("L", BatchSize.Full, "1")]
    [InlineData("L", BatchSize.Half, "0.5")]
    public async Task Density_enabled_BOM_is_converted_to_the_department_stock_unit_for_production(
        string symbol, BatchSize size, string expected)
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var db = fixture.Harness.Db;
        var mg = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Milligram", Symbol = "mg" };
        var ml = new UnitOfMeasure { Id = Guid.NewGuid(), Name = symbol, Symbol = symbol };
        var densityUnit = new UnitOfMeasure { Name = "Grams per milliliter", Symbol = "g/mL" };
        db.UnitOfMeasures.AddRange(mg, ml, densityUnit);
        fixture.Batch.BatchSize = size;
        fixture.Lot.UoMId = ml.Id;
        fixture.Lot.UoM = ml;
        fixture.Shelf.UoMId = ml.Id;
        var link = await db.MaterialDepartments.SingleAsync(item => item.MaterialId == fixture.Lot.MaterialId);
        link.UoMId = ml.Id;
        link.UoM = ml;
        link.Density = 1.5m;
        link.DensityUoM = densityUnit;
        var item = await db.BillOfMaterialItems.SingleAsync();
        item.BaseUoMId = mg.Id;
        item.BaseUoM = mg;
        item.BaseQuantity = 1_500_000;
        item.PrescribedQuantity = 1_500_000;
        item.UseDensityConversion = true;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var source = await ProductionMaterialSources.Load(db, fixture.Batch.Id, MaterialKind.Raw);
        Assert.True(source.IsSuccess, source.Error.Description);
        var requirement = Assert.Single(source.Value.Requirements);
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), requirement.Quantity);
        Assert.Equal(symbol, requirement.Unit);
        Assert.Equal("mg", requirement.BaseUnit);
        Assert.Equal(1.5m, requirement.DensityGramsPerMilliliter);
        Assert.True(requirement.UseDensityConversion);

        var rows = await fixture.Repository.CheckMaterialStockLevelsForProductionSchedule(fixture.Batch.Id, null);
        Assert.True(rows.IsSuccess, rows.Error.Description);
        Assert.Equal(requirement.Quantity, Assert.Single(rows.Value).QuantityNeeded);
        Assert.Equal(symbol, Assert.Single(rows.Value).MaterialDepartment.UoM.Symbol);

        var verifiedLink = await db.MaterialDepartments.SingleAsync(row => row.MaterialId == fixture.Lot.MaterialId);
        verifiedLink.DensityUoMId = null;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var unverified = await ProductionMaterialSources.Load(db, fixture.Batch.Id, MaterialKind.Raw);
        Assert.True(unverified.IsSuccess, unverified.Error.Description);
        Assert.Empty(unverified.Value.Requirements);
        Assert.Contains(unverified.Value.Blockers, blocker => blocker.Contains("active g/mL unit"));
    }

    [Fact]
    public async Task A_selected_substitute_uses_its_own_density_and_stock_unit()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var db = fixture.Harness.Db;
        var ml = new UnitOfMeasure { Name = "Milliliter", Symbol = "ml" };
        var liter = new UnitOfMeasure { Name = "Liter", Symbol = "L" };
        var densityUnit = new UnitOfMeasure { Name = "Grams per milliliter", Symbol = "g/mL" };
        var substitute = new Material { Name = "Alternate liquid", Code = "ALT-LIQ", Kind = MaterialKind.Raw };
        db.UnitOfMeasures.AddRange(ml, liter, densityUnit);
        var primary = await db.MaterialDepartments.SingleAsync();
        primary.UoM = ml;
        primary.DensityUoM = densityUnit;
        primary.Density = 1.5m;
        db.MaterialDepartments.Add(new()
        {
            Material = substitute, DepartmentId = fixture.Department.Id, UoM = liter,
            DensityUoM = densityUnit, Density = 1.25m
        });
        var item = await db.BillOfMaterialItems.SingleAsync();
        item.UseDensityConversion = true;
        item.PrescribedQuantity = 1_000_000m;
        // The substitute's own quantity, in mg like the main line, converted with its own density.
        item.Substitutes.Add(new() { SubstituteMaterial = substitute, BaseUoMId = fixture.Lot.UoMId, BaseQuantity = 1m, PrescribedQuantity = 1_000_000m });
        db.Requisitions.Add(new()
        {
            ProductionScheduleProductId = fixture.Batch.Id, RequisitionType = RequisitionType.Stock,
            Items = [new() { Material = substitute, UoM = liter, Quantity = 0.8m }]
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var loaded = await ProductionMaterialSources.Load(db, fixture.Batch.Id, MaterialKind.Raw);
        Assert.True(loaded.IsSuccess, loaded.Error.Description);
        var requirement = Assert.Single(loaded.Value.Requirements);
        Assert.Equal(fixture.Lot.MaterialId, requirement.SourceMaterialId);
        Assert.Equal(substitute.Id, requirement.MaterialId);
        Assert.Equal(liter.Id, requirement.UnitId);
        Assert.Equal(0.8m, requirement.Quantity);
        Assert.Equal(1.25m, requirement.DensityGramsPerMilliliter);
    }

    [Fact]
    public async Task Density_requisition_restores_the_exact_snapshot_after_browser_round_trip()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var db = fixture.Harness.Db;
        var requirement = fixture.Requirement() with
        {
            Quantity = 666.66666666666666666666666667m,
            UseDensityConversion = true
        };
        var configuration = fixture.Configuration();
        configuration.RequirementsJson = ProductionMaterialSources.Json(new[] { requirement });
        db.ProductionMaterialConfigurations.Add(configuration);
        await db.SaveChangesAsync();

        var line = new DOMAIN.Entities.Requisitions.Request.CreateRequisitionItemRequest
        {
            MaterialId = requirement.MaterialId, UomId = requirement.UnitId, Quantity = 666.6666666666666m
        };
        var accepted = await ProductionMaterialRequisitionRules.Validate(db, fixture.Batch.Id, [line]);
        Assert.True(accepted.IsSuccess, accepted.Error.Description);
        Assert.Equal(requirement.Quantity, line.Quantity);

        line.Quantity = 666.67m;
        var refused = await ProductionMaterialRequisitionRules.Validate(db, fixture.Batch.Id, [line]);
        Assert.Equal("Requisition.StaleConfiguration", refused.Error.Code);
        Assert.Equal(666.67m, line.Quantity);
    }

    [Fact]
    public async Task Stock_level_updates_preserve_the_tagged_density()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var db = fixture.Harness.Db;
        var link = await db.MaterialDepartments.SingleAsync();
        link.Density = 1450m;
        link.DensityUoM = new UnitOfMeasure { Name = "Kilograms per cubic meter", Symbol = "kg/m³" };
        await db.SaveChangesAsync();
        var savedUnit = link.DensityUoMId;
        var repository = new MaterialRepository(db, fixture.Mapper);
        var result = await repository.CreateMaterialDepartment(
            [new() { MaterialId = link.MaterialId, UoMId = link.UoMId, Density = 1.47m, MaximumStockLevel = 700m }],
            fixture.Actor.Id);
        Assert.True(result.IsSuccess, result.Error.Description);
        db.ChangeTracker.Clear();
        var saved = await db.MaterialDepartments.SingleAsync();
        Assert.Equal(1450m, saved.Density);
        Assert.Equal(savedUnit, saved.DensityUoMId);
        Assert.Equal(700m, saved.MaximumStockLevel);
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
