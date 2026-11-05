using System.Reflection;
using API.Controllers;
using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>
/// The warehouse's monthly production material forecast: one batch, all batches, what free stock can cover
/// and how many whole batches that is, using the production start rules for requirements and stock.
/// </summary>
public class ProductionMaterialForecastTests
{
    private static readonly DateTime November = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    private static async Task<ScheduleStockFixture> Seeded(int batches)
    {
        var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        fixture.Schedule.ScheduledStartTime = November.AddDays(2);
        fixture.Schedule.ScheduledEndTime = November.AddDays(20);
        fixture.Batch.BatchNumber = "B1";
        for (var number = 2; number <= batches; number++)
            fixture.Harness.Db.ProductionScheduleProducts.Add(new ProductionScheduleProduct { Id = Guid.NewGuid(),
                ProductId = fixture.Batch.ProductId, ProductionScheduleId = fixture.Schedule.Id, BatchSize = BatchSize.Full, BatchNumber = $"B{number}" });
        await fixture.Harness.Db.SaveChangesAsync();
        return fixture;
    }

    private static async Task<ProductionMaterialForecastDto> Forecast(ScheduleStockFixture fixture)
    {
        fixture.Harness.Db.ChangeTracker.Clear();
        var result = await ProductionMaterialForecasts.Build(fixture.Harness.Db, fixture.Department.Id, 2026, 11);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        return result.Value;
    }

    /// <summary>Adds a product whose BOM needs <paramref name="quantity"/> of the fixture's material, with its own schedule.</summary>
    private static async Task<Product> AddProduct(ScheduleStockFixture fixture, string code, decimal quantity, DateTime start, int batches)
    {
        var db = fixture.Harness.Db;
        var product = new Product { Id = Guid.NewGuid(), Code = code, Name = code, DepartmentId = fixture.Department.Id,
            BaseUomId = fixture.Lot.UoMId, FullBatchSize = 10 };
        var bom = new BillOfMaterial { Id = Guid.NewGuid(), Product = product, IsActive = true, Items =
            [new() { Id = Guid.NewGuid(), MaterialId = fixture.Lot.MaterialId, BaseUoMId = fixture.Lot.UoMId, BaseQuantity = 10, PrescribedQuantity = quantity }] };
        db.ProductBillOfMaterials.Add(new() { Product = product, BillOfMaterial = bom, IsActive = true, EffectiveDate = DateTime.UtcNow.AddDays(-1) });
        var schedule = new ProductionSchedule { Id = Guid.NewGuid(), Code = $"PS-{code}", DepartmentId = fixture.Department.Id,
            CreatedById = fixture.Creator.Id, ScheduledStartTime = start, ScheduledEndTime = start.AddDays(5) };
        db.ProductionSchedules.Add(schedule);
        for (var number = 1; number <= batches; number++)
            db.ProductionScheduleProducts.Add(new ProductionScheduleProduct { Id = Guid.NewGuid(), Product = product,
                ProductionSchedule = schedule, BatchSize = BatchSize.Full, BatchNumber = $"{code}-{number}" });
        await db.SaveChangesAsync();
        return product;
    }

    [Fact]
    public void Endpoint_has_its_own_warehouse_permission()
    {
        var method = typeof(ReportController).GetMethod(nameof(ReportController.GetProductionMaterialForecast))
            ?? throw new InvalidOperationException("Missing forecast endpoint");
        Assert.Equal(PermissionKeys.CanViewProductionScheduleMaterialForecast,
            Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        var permission = Assert.Single(PermissionUtils.GeneratePermissions(), item => item.Key == PermissionKeys.CanViewProductionScheduleMaterialForecast);
        Assert.Equal(PermissionModules.Warehouse, permission.Module);
    }

    [Fact]
    public async Task Forecast_gives_one_batch_all_batches_what_stock_covers_and_whole_batches()
    {
        using var fixture = await Seeded(4);

        var report = await Forecast(fixture);

        Assert.Equal(4, report.TotalBatches);
        var schedule = Assert.Single(report.Schedules);
        Assert.Equal(["B1", "B2", "B3", "B4"], schedule.Batches.Select(item => item.BatchNumber));
        var product = Assert.Single(report.Products);
        Assert.Equal(4, product.BatchCount);
        var material = Assert.Single(product.RawMaterials);
        Assert.Equal(fixture.Lot.MaterialId, material.MaterialId);
        Assert.Equal("mg", material.Unit);
        Assert.Equal(300_000, material.PerBatchQuantity);
        Assert.Equal(1_200_000, material.TotalQuantity);
        // The lot is past expiry but within its retest date, so it is usable.
        Assert.Equal(1_000_000, material.FreeInWarehouse);
        Assert.Equal(1_000_000, material.CanSatisfyQuantity);
        Assert.Equal(200_000, material.ShortfallQuantity);
        Assert.Equal(3, material.BatchesSatisfiable);
        Assert.Equal(3, product.RawBatchesSatisfiable);
        Assert.Equal([true, true, true, false], schedule.Batches.Select(item => item.RawReady));
        // No packing style: packaging cannot be forecast, so it is reported rather than shown as ready.
        Assert.Equal(0, product.PackagingBatchesSatisfiable);
        Assert.Contains(product.Warnings, warning => warning.StartsWith("Packaging:"));
    }

    [Fact]
    public async Task Half_batches_need_half_the_quantity_and_are_a_separate_line()
    {
        using var fixture = await Seeded(2);
        fixture.Harness.Db.ProductionScheduleProducts.Add(new ProductionScheduleProduct { Id = Guid.NewGuid(),
            ProductId = fixture.Batch.ProductId, ProductionScheduleId = fixture.Schedule.Id, BatchSize = BatchSize.Half, BatchNumber = "H1" });
        await fixture.Harness.Db.SaveChangesAsync();

        var report = await Forecast(fixture);

        Assert.Equal(2, report.Products.Count);
        var half = Assert.Single(report.Products, item => item.BatchSize == BatchSize.Half);
        Assert.Equal(150_000, Assert.Single(half.RawMaterials).PerBatchQuantity);
        Assert.Equal(2, Assert.Single(report.Products, item => item.BatchSize == BatchSize.Full).BatchCount);
    }

    [Fact]
    public async Task Reserved_batches_are_covered_and_other_reservations_reduce_free_stock()
    {
        using var fixture = await Seeded(4);
        var db = fixture.Harness.Db;
        // B1 started: its stock is reserved and its applied requirement is used.
        db.ProductionMaterialConfigurations.Add(fixture.Configuration());
        db.MaterialBatchReservedQuantities.Add(new() { Id = Guid.NewGuid(), MaterialBatchId = fixture.Lot.Id,
            ProductionScheduleProductId = fixture.Batch.Id, WarehouseId = fixture.Production.Id, UoMId = fixture.Lot.UoMId,
            WarehouseLocationShelfId = fixture.Shelf.WarehouseLocationShelfId, Quantity = 300_000 });
        // A batch outside this month's plan holds 200 000 mg too.
        var later = new ProductionSchedule { Id = Guid.NewGuid(), Code = "PS-DEC", DepartmentId = fixture.Department.Id,
            CreatedById = fixture.Creator.Id, ScheduledStartTime = November.AddMonths(1), ScheduledEndTime = November.AddMonths(1).AddDays(3) };
        var holder = new ProductionScheduleProduct { Id = Guid.NewGuid(), ProductId = fixture.Batch.ProductId,
            ProductionSchedule = later, BatchSize = BatchSize.Full, BatchNumber = "DEC-1" };
        db.ProductionScheduleProducts.Add(holder);
        db.MaterialBatchReservedQuantities.Add(new() { Id = Guid.NewGuid(), MaterialBatchId = fixture.Lot.Id,
            ProductionScheduleProductId = holder.Id, WarehouseId = fixture.Production.Id, UoMId = fixture.Lot.UoMId,
            WarehouseLocationShelfId = fixture.Shelf.WarehouseLocationShelfId, Quantity = 200_000 });
        await db.SaveChangesAsync();

        var report = await Forecast(fixture);

        Assert.Equal(4, report.TotalBatches);
        var material = Assert.Single(Assert.Single(report.Products).RawMaterials);
        Assert.Equal(500_000, material.FreeInWarehouse);
        Assert.Equal(300_000, material.ReservedQuantity);
        Assert.Equal(800_000, material.CanSatisfyQuantity);
        Assert.Equal(400_000, material.ShortfallQuantity);
        Assert.Equal(2, material.BatchesSatisfiable);
        var batches = Assert.Single(report.Schedules).Batches;
        Assert.True(batches[0].RawReserved);
        Assert.Equal([true, true, false, false], batches.Select(item => item.RawReady));
    }

    [Fact]
    public async Task Stock_past_its_retest_date_is_not_counted_and_undated_stock_never_expires()
    {
        using var fixture = await Seeded(1);
        fixture.Lot.RetestDate = DateTime.UtcNow.AddDays(-1);
        await fixture.Harness.Db.SaveChangesAsync();
        var expired = Assert.Single(Assert.Single((await Forecast(fixture)).Products).RawMaterials);
        Assert.Equal(0, expired.FreeInWarehouse);
        Assert.Equal(0, expired.BatchesSatisfiable);

        var lot = await fixture.Harness.Db.MaterialBatches.FindAsync(fixture.Lot.Id);
        lot!.ExpiryDate = null;
        lot.RetestDate = null;
        await fixture.Harness.Db.SaveChangesAsync();
        var undated = Assert.Single(Assert.Single((await Forecast(fixture)).Products).RawMaterials);
        Assert.Equal(1_000_000, undated.FreeInWarehouse);
        Assert.Equal(1, undated.BatchesSatisfiable);
    }

    [Fact]
    public async Task A_shared_material_is_shared_out_in_schedule_order_not_counted_twice()
    {
        using var fixture = await Seeded(2);
        await AddProduct(fixture, "SYRUP-LATE", 300_000, November.AddDays(10), 2);

        var report = await Forecast(fixture);

        var first = Assert.Single(Assert.Single(report.Products, item => item.ProductCode == "TABLET-1").RawMaterials);
        var second = Assert.Single(Assert.Single(report.Products, item => item.ProductCode == "SYRUP-LATE").RawMaterials);
        Assert.Equal(600_000, first.CanSatisfyQuantity);
        Assert.Equal(2, first.BatchesSatisfiable);
        Assert.Equal(400_000, second.CanSatisfyQuantity);
        Assert.Equal(1, second.BatchesSatisfiable);
        Assert.Equal(200_000, second.ShortfallQuantity);
        Assert.Equal(1, first.SharedWithProducts);
        Assert.Equal(1_000_000, second.FreeInWarehouse);
    }

    [Fact]
    public async Task Other_months_other_departments_and_cancelled_batches_are_left_out()
    {
        using var fixture = await Seeded(2);
        var db = fixture.Harness.Db;
        await AddProduct(fixture, "OCTOBER", 1, November.AddMonths(-1), 1);
        db.ProductionSchedules.Add(new ProductionSchedule { Id = Guid.NewGuid(), Code = "PS-SYRUP", DepartmentId = fixture.OtherDepartment.Id,
            CreatedById = fixture.Creator.Id, ScheduledStartTime = November.AddDays(3), ScheduledEndTime = November.AddDays(4),
            Products = [new ProductionScheduleProduct { Id = Guid.NewGuid(), ProductId = fixture.Batch.ProductId, BatchNumber = "OTHER" }] });
        db.ProductionScheduleProducts.Add(new ProductionScheduleProduct { Id = Guid.NewGuid(), ProductId = fixture.Batch.ProductId,
            ProductionScheduleId = fixture.Schedule.Id, BatchNumber = "CANCELLED", Cancelled = true });
        await db.SaveChangesAsync();

        var report = await Forecast(fixture);

        Assert.Equal(["B1", "B2"], report.Schedules.SelectMany(item => item.Batches).Select(item => item.BatchNumber));
    }

    [Fact]
    public async Task Invalid_month_is_rejected()
    {
        using var fixture = await Seeded(1);
        var result = await ProductionMaterialForecasts.Build(fixture.Harness.Db, fixture.Department.Id, 2026, 13);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Repository_requires_the_live_grant_and_reports_the_users_own_department()
    {
        using var fixture = await Seeded(1);
        var db = fixture.Harness.Db;
        var repository = new ReportRepository(db, fixture.Mapper, null!, NullLogger<ReportRepository>.Instance);
        var filter = new ProductionMaterialForecastFilter { Year = 2026, Month = 11 };

        var denied = await repository.GetProductionMaterialForecast(filter, fixture.Actor.Id);
        Assert.Equal(ErrorType.Forbidden, denied.Error.Type);

        var role = new Role { Id = Guid.NewGuid(), Name = "Forecast role" };
        db.Roles.Add(role);
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = fixture.Actor.Id, RoleId = role.Id });
        var claim = new IdentityRoleClaim<Guid> { Id = 4321, RoleId = role.Id, ClaimType = AppConstants.Permission,
            ClaimValue = PermissionKeys.CanViewProductionScheduleMaterialForecast };
        db.RoleClaims.Add(claim);
        db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = claim.Id, Key = claim.ClaimValue, Type = "Action" });
        await db.SaveChangesAsync();

        var allowed = await repository.GetProductionMaterialForecast(filter, fixture.Actor.Id);
        Assert.True(allowed.IsSuccess, allowed.IsFailure ? allowed.Error.Description : null);
        Assert.Equal(fixture.Department.Id, allowed.Value.DepartmentId);
        Assert.Equal(1, allowed.Value.TotalBatches);
    }
}
