using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.Packing;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Reporting;

/// <summary>The production monthly data (three reports) and the stock issued report.</summary>
public class ProductionMonthlyReportTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "NonProduction";
    }
    private static ApplicationDbContext Memory() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());
    private static ReportRepository Repository(ApplicationDbContext db) => new(db, null!, null!, NullLogger<ReportRepository>.Instance);
    private static DateTime Utc(int month, int day) => new(2026, month, day, 0, 0, 0, DateTimeKind.Utc);
    private static ProductionMonthlyFilter November(Guid? department = null) => new() { Year = 2026, Month = 11, DepartmentId = department };

    private static async Task<(User Actor, Department Production)> Seed(ApplicationDbContext db, string? key, bool production = false)
    {
        var department = new Department { Id = Guid.NewGuid(), Name = "Tablets", Type = DepartmentType.Production };
        var office = new Department { Id = Guid.NewGuid(), Name = "Planning", Type = DepartmentType.NonProduction };
        db.Departments.AddRange(department, office);
        var actor = new User { Id = Guid.NewGuid(), UserName = "planner", FirstName = "Plan", LastName = "Ner", DepartmentId = production ? department.Id : office.Id };
        db.Users.Add(actor);
        if (key is not null)
        {
            var role = new Role { Id = Guid.NewGuid(), Name = "Reports" }; db.Roles.Add(role);
            db.UserRoles.Add(new IdentityUserRole<Guid> { RoleId = role.Id, UserId = actor.Id });
            var claim = new IdentityRoleClaim<Guid> { RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key };
            db.RoleClaims.Add(claim); await db.SaveChangesAsync();
            db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = claim.Id, Key = key, Type = "View" });
        }
        await db.SaveChangesAsync();
        return (actor, department);
    }

    private static ProductionScheduleProduct Batch(ApplicationDbContext db, Department department, DateTime start, string batch,
        BatchSize size = BatchSize.Full, Product? product = null)
    {
        product ??= new Product { Id = Guid.NewGuid(), Code = "TAB-1", Name = "Paracetamol 500 mg", DepartmentId = department.Id,
            Prices = [new ProductPrices { Price = 10m, PriceUoM = "Pack", Date = Utc(1, 1) }, new ProductPrices { Price = 12m, PriceUoM = "Pack", Date = Utc(12, 1) }] };
        var packing = new ProductPacking { Id = Guid.NewGuid(), Name = "10 × 10", Product = product, ExpectedYield = 1001m, PackPerShipper = 50 };
        var schedule = new ProductionSchedule { Id = Guid.NewGuid(), Code = "PLAN-11", ScheduledStartTime = start, ScheduledEndTime = start.AddDays(5) };
        var item = new ProductionScheduleProduct { Id = Guid.NewGuid(), Product = product, ProductionSchedule = schedule, ProductPacking = packing,
            BatchNumber = batch, BatchSize = size, MarketType = new Customer { Id = Guid.NewGuid(), Name = "Local" } };
        db.ProductionScheduleProducts.Add(item);
        return item;
    }

    [Fact]
    public async Task One_row_per_batch_with_expected_packed_retained_transferred_and_the_price_of_that_month()
    {
        await using var db = Memory();
        var (actor, department) = await Seed(db, PermissionKeys.CanViewProductionMonthlyReport);
        var full = Batch(db, department, Utc(11, 3), "PLANNED-1");
        var half = Batch(db, department, Utc(11, 20), "PLANNED-2", BatchSize.Half, full.Product);
        Batch(db, department, Utc(12, 1), "DECEMBER");
        var cancelled = Batch(db, department, Utc(11, 5), "CANCELLED"); cancelled.Cancelled = true;
        var step = new ProductionActivityStep { Id = Guid.NewGuid(), ProductionActivity = new ProductionActivity { Id = Guid.NewGuid(), ProductionScheduleProduct = full },
            Operation = new Operation { Id = Guid.NewGuid(), Name = "BMR issue" } };
        var record = new BatchManufacturingRecord { Id = Guid.NewGuid(), ProductionScheduleProduct = full, ProductionActivityStep = step, BatchNumber = "B-2611", IssuedDate = Utc(11, 2) };
        db.BatchManufacturingRecords.Add(record);
        db.FinalPackings.Add(new FinalPacking { Id = Guid.NewGuid(), ProductionScheduleProduct = full, TotalQuantityPacked = 980m, RetainedSamples = 6m,
            NumberOfBottlesPerShipper = 50m, NUmberOfFullShipperPacked = 19m, LeftOver = 30m });
        var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Packs", Symbol = "pk" };
        db.FinishedGoodsTransferNotes.AddRange(new FinishedGoodsTransferNote { Id = Guid.NewGuid(), BatchManufacturingRecord = record, TotalQuantity = 900m, UoM = unit },
            new FinishedGoodsTransferNote { Id = Guid.NewGuid(), BatchManufacturingRecord = record, TotalQuantity = 70m, UoM = unit });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();

        var result = await Repository(db).GetProductionMonthlyReport(November(), actor.Id);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(error => error.Description)));
        Assert.Equal(new[] { full.Id, half.Id }, result.Value.Rows.Select(row => row.ScheduleProductId));
        var first = result.Value.Rows[0];
        Assert.Equal("B-2611", first.BatchNumber);
        Assert.Equal(1001m, first.ExpectedYield);
        Assert.True(first.Packed); Assert.Equal(980m, first.ActualYield); Assert.Equal(6m, first.RetainedSamples);
        Assert.Equal(970m, first.TransferredToWarehouse); Assert.Equal("pk", first.TransferUnit); Assert.Equal(2, first.TransferNotes);
        Assert.Equal(10m, first.UnitPrice); Assert.Equal("Local", first.MarketName); Assert.Equal(50, first.PacksPerShipper);
        var second = result.Value.Rows[1];
        Assert.Equal("PLANNED-2", second.BatchNumber);
        Assert.Equal(501m, second.ExpectedYield); Assert.Equal(0.5m, second.BatchShare);
        Assert.False(second.Packed); Assert.Null(second.ActualYield); Assert.Null(second.TransferredToWarehouse);
    }

    [Fact]
    public async Task Any_of_the_three_production_keys_opens_it_and_other_keys_do_not()
    {
        foreach (var key in new[] { PermissionKeys.CanViewProductionScheduleSummaryReport, PermissionKeys.CanViewProductionBatchValueReport })
        {
            await using var db = Memory(); var (actor, _) = await Seed(db, key);
            Assert.True((await Repository(db).GetProductionMonthlyReport(November(), actor.Id)).IsSuccess, key);
        }
        foreach (var key in new[] { PermissionKeys.CanViewQcAndQaReport, PermissionKeys.CanViewStockIssuedReport, null })
        {
            await using var db = Memory(); var (actor, _) = await Seed(db, key);
            Assert.Equal(ErrorType.Forbidden, Assert.Single((await Repository(db).GetProductionMonthlyReport(November(), actor.Id)).Errors).Type);
        }
    }

    [Fact]
    public async Task Production_users_see_only_their_department_and_a_month_is_required()
    {
        await using var db = Memory();
        var (actor, department) = await Seed(db, PermissionKeys.CanViewProductionMonthlyReport, production: true);
        var other = new Department { Id = Guid.NewGuid(), Name = "Liquids", Type = DepartmentType.Production };
        db.Departments.Add(other);
        var own = Batch(db, department, Utc(11, 3), "OWN");
        Batch(db, other, Utc(11, 3), "OTHER");
        await db.SaveChangesAsync();
        var result = await Repository(db).GetProductionMonthlyReport(November(), actor.Id);
        Assert.Equal(new[] { own.Id }, result.Value.Rows.Select(row => row.ScheduleProductId));
        Assert.Equal(ErrorType.Forbidden, Assert.Single((await Repository(db).GetProductionMonthlyReport(November(other.Id), actor.Id)).Errors).Type);
        Assert.Equal(ErrorType.Validation, Assert.Single((await Repository(db).GetProductionMonthlyReport(new() { Year = 2026 }, actor.Id)).Errors).Type);
    }

    [Fact]
    public async Task Stock_issued_lists_each_material_batch_issued_and_extra_packing_filtered_by_product_and_batch()
    {
        await using var db = Memory();
        var (actor, department) = await Seed(db, PermissionKeys.CanViewStockIssuedReport);
        var batch = Batch(db, department, Utc(11, 3), "B-1");
        var another = Batch(db, department, Utc(11, 3), "B-2", product: batch.Product);
        var material = new Material { Id = Guid.NewGuid(), Code = "RM-1", Name = "Starch", Kind = MaterialKind.Raw };
        var bottle = new Material { Id = Guid.NewGuid(), Code = "PM-1", Name = "Bottle", Kind = MaterialKind.Package };
        var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Kilogram", Symbol = "kg" };
        var store = new Warehouse { Id = Guid.NewGuid(), Name = "Raw store" };
        var requisition = new Requisition { Id = Guid.NewGuid(), Code = "SR-1", RequisitionType = RequisitionType.Stock, ProductionScheduleProduct = batch, DepartmentId = department.Id, RequestedById = actor.Id };
        var other = new Requisition { Id = Guid.NewGuid(), Code = "SR-2", RequisitionType = RequisitionType.Stock, ProductionScheduleProduct = another, DepartmentId = department.Id, RequestedById = actor.Id };
        db.Requisitions.AddRange(requisition, other);
        db.StockRequisitionIssues.Add(new StockRequisitionIssue { Id = Guid.NewGuid(), Requisition = requisition, IssuedById = actor.Id, IssuedAt = Utc(11, 4) });
        db.StockRequisitionIssues.Add(new StockRequisitionIssue { Id = Guid.NewGuid(), Requisition = other, IssuedById = actor.Id, IssuedAt = Utc(11, 4) });
        BinCardInformation Card(Requisition owner, string lot, decimal issued, decimal received = 0) => new()
        {
            Id = Guid.NewGuid(), MaterialBatch = new MaterialBatch { Id = Guid.NewGuid(), Material = material, BatchNumber = lot },
            QuantityIssued = issued, QuantityReceived = received, UoM = unit, Warehouse = store, RequisitionId = owner.Id,
            RequisitionCode = owner.Code, ProductBatchNumber = owner.ProductionScheduleProduct.BatchNumber, CreatedAt = Utc(11, 4), ArNumber = "AR-" + lot,
        };
        db.BinCardInformation.AddRange(Card(requisition, "L1", 2.5m), Card(requisition, "L2", 1m), Card(requisition, "L3", 0, 3.5m), Card(other, "L4", 4m));
        db.ProductionExtraPackings.Add(new ProductionExtraPacking { Id = Guid.NewGuid(), ProductionScheduleProduct = batch, Material = bottle, UoM = unit,
            Quantity = 20m, IssuedAt = Utc(11, 6), IssuedBy = new User { Id = Guid.NewGuid(), UserName = "store", FirstName = "Store", LastName = "Keeper" } });
        db.ProductionExtraPackings.Add(new ProductionExtraPacking { Id = Guid.NewGuid(), ProductionScheduleProduct = batch, Material = bottle, UoM = unit, Quantity = 5m });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();

        var all = await Repository(db).GetStockIssuedReport(new(), actor.Id);
        Assert.True(all.IsSuccess, string.Join("; ", all.Errors.Select(error => error.Description)));
        Assert.Equal(4, all.Value.Rows.Count);
        var only = await Repository(db).GetStockIssuedReport(new() { ProductId = batch.ProductId, BatchNumber = " b-1 " }, actor.Id);
        Assert.Equal(new[] { "L1", "L2", null }, only.Value.Rows.Select(row => row.MaterialBatchNumber));
        Assert.Equal(new[] { 2.5m, 1m, 20m }, only.Value.Rows.Select(row => row.Quantity));
        Assert.Equal("Plan Ner", only.Value.Rows[0].IssuedByName);
        Assert.Equal(StockIssueSource.ExtraPacking, only.Value.Rows[2].Source);
        Assert.Equal("Store Keeper", only.Value.Rows[2].IssuedByName);
        var dated = await Repository(db).GetStockIssuedReport(new() { StartDate = new(2026, 11, 5), EndDate = new(2026, 11, 30) }, actor.Id);
        Assert.Equal(new[] { StockIssueSource.ExtraPacking }, dated.Value.Rows.Select(row => row.Source));
        Assert.Equal(ErrorType.Validation, Assert.Single((await Repository(db).GetStockIssuedReport(new() { StartDate = new(2026, 11, 5) }, actor.Id)).Errors).Type);
    }

    [Fact]
    public async Task Stock_issued_needs_its_own_key()
    {
        await using var db = Memory(); var (actor, _) = await Seed(db, PermissionKeys.CanViewWarehouseReport);
        Assert.Equal(ErrorType.Forbidden, Assert.Single((await Repository(db).GetStockIssuedReport(new(), actor.Id)).Errors).Type);
    }
}
