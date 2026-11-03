using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Reporting;

public class IssuedBmrReportTests
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
    private static IssuedBmrReportFilter Filter(Guid? departmentId = null) => new()
        { StartDate = new(2026, 11, 1), EndDate = new(2026, 11, 2), DepartmentId = departmentId };
    private static DateTime Utc(int day) => new(2026, 11, day, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Issue_dates_include_the_whole_end_day_and_completed_archived_cancelled_history()
    {
        await using var db = Memory();
        await VerifyReport(db);
    }

    private static async Task VerifyReport(ApplicationDbContext db)
    {
        var actor = await Seed(db);
        var first = AddRecord(db, actor.DepartmentId, Utc(1));
        first.CreatedAt = Utc(1).AddMonths(-1); // Request date does not control inclusion.
        first.IssuedBy = actor; first.IssuedById = actor.Id;
        first.ProductionActivityStep.CompletedAt = Utc(1);
        var last = AddRecord(db, actor.DepartmentId, Utc(3).AddTicks(-10));
        last.DeletedAt = Utc(3);
        last.ProductionScheduleProduct.Cancelled = true;
        last.ProductionScheduleProduct.Product.DeletedAt = Utc(3);
        last.ProductionScheduleProduct.ProductionSchedule.DeletedAt = Utc(3);
        AddRecord(db, actor.DepartmentId, Utc(1).AddTicks(-10));
        AddRecord(db, actor.DepartmentId, Utc(3));
        AddRecord(db, actor.DepartmentId, null).Status = BatchManufacturingStatus.Approved;
        var other = new Department { Id = Guid.NewGuid(), Name = "Other production", Type = DepartmentType.Production };
        db.Departments.Add(other);
        var otherRecord = AddRecord(db, other.Id, Utc(2));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var result = await Repository(db).GetIssuedBmrReport(Filter(actor.DepartmentId), actor.Id);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(error => error.Description)));
        var report = result.Value;
        Assert.Equal(2, report.TotalIssued);
        Assert.Equal(new[] { first.Id, last.Id }, report.Rows.Select(row => row.Id));
        Assert.Equal("QA Operator", report.Rows[0].IssuedByName);
        Assert.Null(report.Rows[1].IssuedById); Assert.Null(report.Rows[1].IssuedByName);
        Assert.True(report.Rows[1].IsArchived); Assert.True(report.Rows[1].IsCancelled);
        Assert.Equal(0.00001m, report.Rows[0].BatchQuantity);
        Assert.Equal("mg", report.Rows[0].BatchUnit);
        Assert.Empty(db.ChangeTracker.Entries());
        var all = await Repository(db).GetIssuedBmrReport(Filter(), actor.Id);
        Assert.True(all.IsSuccess); Assert.Equal(3, all.Value.TotalIssued);
        Assert.Contains(all.Value.Rows, row => row.Id == otherRecord.Id);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task Missing_permission_or_disabled_actor_is_denied(bool grant, bool disabled)
    {
        await using var db = Memory();
        var actor = await Seed(db, grant, disabled);
        var result = await Repository(db).GetIssuedBmrReport(Filter(), actor.Id);
        Assert.Equal(ErrorType.Forbidden, Assert.Single(result.Errors).Type);
    }

    [Fact]
    public async Task Revoked_permission_is_not_reused_and_production_scope_cannot_escape_its_department()
    {
        await using var db = Memory();
        var actor = await Seed(db, production: true);
        AddRecord(db, actor.DepartmentId, Utc(1));
        await db.SaveChangesAsync();
        var own = await Repository(db).GetIssuedBmrReport(Filter(), actor.Id);
        Assert.True(own.IsSuccess); Assert.Equal(actor.DepartmentId, own.Value.DepartmentId);
        Assert.Equal(ErrorType.Forbidden, Assert.Single((await Repository(db)
            .GetIssuedBmrReport(Filter(Guid.NewGuid()), actor.Id)).Errors).Type);
        db.PermissionTypes.RemoveRange(db.PermissionTypes); await db.SaveChangesAsync();
        Assert.Equal(ErrorType.Forbidden, Assert.Single((await Repository(db).GetIssuedBmrReport(Filter(), actor.Id)).Errors).Type);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public async Task Missing_reversed_overflow_and_invalid_department_filters_are_refused(int kind)
    {
        await using var db = Memory(); var actor = await Seed(db); var filter = Filter();
        if (kind == 0) filter.StartDate = null;
        if (kind == 1) filter.EndDate = null;
        if (kind == 2) filter.EndDate = filter.StartDate?.AddDays(-1);
        if (kind == 3) filter.EndDate = DateOnly.MaxValue;
        if (kind == 4) filter.DepartmentId = Guid.Empty;
        Assert.Equal(ErrorType.Validation, Assert.Single((await Repository(db).GetIssuedBmrReport(filter, actor.Id)).Errors).Type);
    }

    [Fact]
    public async Task Empty_period_is_a_valid_complete_report_and_cancellation_is_propagated()
    {
        await using var db = Memory(); var actor = await Seed(db);
        var result = await Repository(db).GetIssuedBmrReport(Filter(), actor.Id);
        Assert.True(result.IsSuccess); Assert.Equal(0, result.Value.TotalIssued); Assert.Empty(result.Value.Rows);
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Repository(db).GetIssuedBmrReport(Filter(), actor.Id, cancel.Token));
    }

    [LocalIssuedBmrFact]
    public async Task PostgreSQL_executes_the_historical_projection_with_nullable_issuer_and_date_boundaries()
    {
        var connection = Environment.GetEnvironmentVariable("ORYX_ISSUED_BMR_TEST_CONNECTION")!;
        var parsed = new NpgsqlConnectionStringBuilder(connection);
        if (parsed.Host is not ("127.0.0.1" or "localhost" or "/tmp") || parsed.Database != "oryx_bmr_issued_report_test")
            throw new InvalidOperationException("Only the dedicated local disposable report database is permitted.");
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options, new Actor());
        await db.Database.EnsureDeletedAsync(); await db.Database.EnsureCreatedAsync();
        await VerifyReport(db);
    }

    private static async Task<User> Seed(ApplicationDbContext db, bool grant = true, bool disabled = false, bool production = false)
    {
        var department = new Department { Id = Guid.NewGuid(), Name = "Production", Type = DepartmentType.Production };
        var qa = new Department { Id = Guid.NewGuid(), Name = "QA", Type = DepartmentType.NonProduction };
        db.Departments.AddRange(department, qa);
        var actor = new User { Id = Guid.NewGuid(), UserName = "qa-reader", FirstName = "QA", LastName = "Operator",
            DepartmentId = production ? department.Id : qa.Id, IsDisabled = disabled };
        db.Users.Add(actor);
        if (grant)
        {
            var role = new Role { Id = Guid.NewGuid(), Name = "Report reader" }; db.Roles.Add(role);
            db.UserRoles.Add(new IdentityUserRole<Guid> { RoleId = role.Id, UserId = actor.Id });
            var claim = new IdentityRoleClaim<Guid> { RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = PermissionKeys.CanViewQcAndQaReport };
            db.RoleClaims.Add(claim); await db.SaveChangesAsync();
            db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = claim.Id, Key = claim.ClaimValue, Type = "View" });
        }
        await db.SaveChangesAsync();
        return new User { Id = actor.Id, DepartmentId = department.Id };
    }

    private static BatchManufacturingRecord AddRecord(ApplicationDbContext db, Guid? departmentId, DateTime? issue)
    {
        var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Milligrams", Symbol = "mg" };
        var product = new Product { Id = Guid.NewGuid(), Code = "P-1", Name = "Example product", DepartmentId = departmentId, BaseUoM = unit };
        var packing = new ProductPacking { Id = Guid.NewGuid(), Name = "10 × 10", Product = product };
        var schedule = new ProductionSchedule { Id = Guid.NewGuid(), Code = "PLAN-1" };
        var batch = new ProductionScheduleProduct { Id = Guid.NewGuid(), Product = product, ProductionSchedule = schedule, ProductPacking = packing };
        var activity = new ProductionActivity { Id = Guid.NewGuid(), ProductionScheduleProduct = batch };
        var step = new ProductionActivityStep { Id = Guid.NewGuid(), ProductionActivity = activity,
            Operation = new Operation { Id = Guid.NewGuid(), Name = "BMR issue" } };
        var record = new BatchManufacturingRecord { Id = Guid.NewGuid(), BatchNumber = "B-001", BatchQuantity = 0.00001m,
            ProductionScheduleProduct = batch, ProductionActivityStep = step, IssuedDate = issue };
        db.BatchManufacturingRecords.Add(record); return record;
    }
}

public sealed class LocalIssuedBmrFactAttribute : FactAttribute
{
    public LocalIssuedBmrFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORYX_ISSUED_BMR_TEST_CONNECTION")))
            Skip = "Set ORYX_ISSUED_BMR_TEST_CONNECTION to the dedicated disposable local database.";
    }
}
