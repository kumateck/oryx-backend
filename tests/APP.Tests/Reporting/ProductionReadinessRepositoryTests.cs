using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products;
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

public class ProductionReadinessRepositoryTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "NonProduction";
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task Missing_permission_and_disabled_users_are_denied(bool granted, bool disabled)
    {
        await using var context = Memory();
        var actor = await Seed(context, granted, disabled);
        var result = await Repository(context).GetProductionReadinessReport(Filter(actor.DepartmentId), actor.Id);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, Assert.Single(result.Errors).Type);
    }

    [Fact]
    public async Task Revoked_grant_is_not_reused_from_an_earlier_request()
    {
        await using var context = Memory();
        var actor = await Seed(context);
        Assert.True((await Repository(context).GetProductionReadinessReport(Filter(actor.DepartmentId), actor.Id)).IsSuccess);
        context.PermissionTypes.RemoveRange(context.PermissionTypes);
        await context.SaveChangesAsync();
        Assert.False((await Repository(context).GetProductionReadinessReport(Filter(actor.DepartmentId), actor.Id)).IsSuccess);
    }

    [Fact]
    public async Task Production_users_cannot_select_another_department()
    {
        await using var context = Memory();
        var actor = await Seed(context, productionActor: true);
        var other = Department("Other", DepartmentType.Production);
        context.Departments.Add(other);
        await context.SaveChangesAsync();
        var denied = await Repository(context).GetProductionReadinessReport(Filter(other.Id), actor.Id);
        Assert.False(denied.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, Assert.Single(denied.Errors).Type);
        Assert.True((await Repository(context).GetProductionReadinessReport(Filter(actor.DepartmentId), actor.Id)).IsSuccess);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Missing_and_reversed_parameters_are_rejected(int variant)
    {
        await using var context = Memory();
        var actor = await Seed(context);
        var filter = Filter(actor.DepartmentId);
        if (variant == 0) filter.DepartmentId = null;
        if (variant == 1) filter.StartDate = null;
        if (variant == 2) filter.EndDate = null;
        if (variant == 3) filter.EndDate = filter.StartDate?.AddDays(-1);
        var result = await Repository(context).GetProductionReadinessReport(filter, actor.Id);
        Assert.Equal(ErrorType.Validation, Assert.Single(result.Errors).Type);
    }

    [Fact]
    public async Task Scope_and_boundary_selection_exclude_cancelled_deleted_and_other_department_rows()
    {
        await using var context = Memory();
        await VerifySelection(context);
    }

    [Fact]
    public async Task Invalid_batch_size_and_wrong_packing_owner_fail_instead_of_exporting_false_totals()
    {
        await using var context = Memory();
        var actor = await Seed(context);
        var item = AddBatch(context, actor.DepartmentId);
        item.BatchSize = (BatchSize)42;
        await context.SaveChangesAsync();
        Assert.Equal(ErrorType.Conflict, Assert.Single((await Repository(context)
            .GetProductionReadinessReport(Filter(actor.DepartmentId), actor.Id)).Errors).Type);
        item.BatchSize = BatchSize.Full;
        var other = new Product { Id = Guid.NewGuid(), Name = "Other", DepartmentId = actor.DepartmentId };
        context.Products.Add(other);
        item.ProductPacking.ProductId = other.Id;
        item.ProductPacking.Product = other;
        await context.SaveChangesAsync();
        Assert.Equal(ErrorType.Conflict, Assert.Single((await Repository(context)
            .GetProductionReadinessReport(Filter(actor.DepartmentId), actor.Id)).Errors).Type);
    }

    [Fact]
    public async Task Cancellation_is_propagated_without_a_partial_report()
    {
        await using var context = Memory();
        var actor = await Seed(context);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Repository(context)
            .GetProductionReadinessReport(Filter(actor.DepartmentId), actor.Id, cancellation.Token));
    }

    [LocalProductionReadinessFact]
    public async Task PostgreSQL_executes_the_complete_projection_and_preserves_the_same_scope()
    {
        var connection = Environment.GetEnvironmentVariable("ORYX_READINESS_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Missing disposable fixture connection.");
        var parsed = new NpgsqlConnectionStringBuilder(connection);
        if (parsed.Host is not ("127.0.0.1" or "localhost")
            || parsed.Database != "oryx_readiness_test")
            throw new InvalidOperationException("Only the dedicated local disposable fixture is permitted.");
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options, new Actor());
        await context.Database.EnsureCreatedAsync();
        await VerifySelection(context);
    }

    private static async Task VerifySelection(ApplicationDbContext context)
    {
        var actor = await Seed(context);
        var valid = AddBatch(context, actor.DepartmentId);
        valid.BatchSize = BatchSize.Half;
        valid.Product.Prices = [new() { Date = Utc(1), Price = 9, PriceUoM = "old" },
            new() { Date = Utc(20), Price = 2, PriceUoM = "piece" }];
        // An overlapping schedule is included even if it began before the selected day.
        valid.ProductionSchedule.ScheduledStartTime = Utc(27);
        AddBatch(context, actor.DepartmentId).Cancelled = true;
        AddBatch(context, actor.DepartmentId).ProductionSchedule.DeletedAt = Utc(28);
        var other = Department("Other", DepartmentType.Production);
        context.Departments.Add(other);
        AddBatch(context, other.Id);
        AddBatch(context, actor.DepartmentId).ProductionSchedule.ScheduledStartTime = Utc(29);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var result = await Repository(context).GetProductionReadinessReport(Filter(actor.DepartmentId), actor.Id);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(error => error.Description)));
        var report = result.Value;
        Assert.Equal(1, report.ProductCount);
        var row = Assert.Single(Assert.Single(report.Markets).Rows);
        Assert.Equal(valid.Id, Assert.Single(row.ScheduleProductIds));
        Assert.Equal(0.5m, row.Batches);
        Assert.Equal(501m, row.ExpectedYield);
        Assert.Equal(1m, row.Shippers);
        Assert.Equal(201m, row.LoosePacks);
        Assert.Equal(2m, row.UnitPrice);
        Assert.Equal("piece", row.PriceUnit);
        Assert.Equal(DefinitionReadiness.Configured, row.Configuration.PackingStyle);
        Assert.Equal(DefinitionReadiness.Missing, row.Configuration.Specification);
        Assert.Equal(ProductReadiness.ActionNeeded, row.Configuration.Readiness);
        Assert.Empty(context.ChangeTracker.Entries());
        await VerifyDefinitions(context, valid.ProductId, actor.Id, actor.DepartmentId);
    }

    private static async Task VerifyDefinitions(ApplicationDbContext context, Guid productId,
        Guid userId, Guid? departmentId)
    {
        var product = await context.Products.IgnoreAutoIncludes().SingleAsync(item => item.Id == productId);
        var form = new DOMAIN.Entities.Forms.Form { Id = Guid.NewGuid(), Name = "Definition" };
        var specification = new DOMAIN.Entities.ProductSpecifications.ProductSpecification
        {
            Id = Guid.NewGuid(), Product = product, ProductId = productId, Form = form, FormId = form.Id,
            UserId = userId, SpecificationNumber = "SPEC"
        };
        context.ProductSpecifications.Add(specification);
        var stp = new DOMAIN.Entities.ProductStandardTestProcedures.ProductStandardTestProcedure
            { Id = Guid.NewGuid(), Product = product, ProductId = productId, StpNumber = "STP" };
        context.ProductStandardTestProcedures.Add(stp);
        context.ProductAnalyticalRawData.Add(new DOMAIN.Entities.ProductAnalyticalRawData.ProductAnalyticalRawData
        {
            Id = Guid.NewGuid(), ProductStandardTestProcedure = stp, StpId = stp.Id,
            Form = form, FormId = form.Id
        });
        context.Routes.Add(new DOMAIN.Entities.Routes.Route
        {
            Id = Guid.NewGuid(), Product = product, ProductId = productId,
            Operation = new DOMAIN.Entities.Base.Operation { Id = Guid.NewGuid(), Name = "Mix" }
        });
        var material = new DOMAIN.Entities.Materials.Material { Id = Guid.NewGuid(), Name = "Material", Code = "M-1" };
        var bom = new DOMAIN.Entities.BillOfMaterials.BillOfMaterial
        {
            Id = Guid.NewGuid(), Product = product, ProductId = productId,
            Items = [new() { Id = Guid.NewGuid(), Material = material, MaterialId = material.Id }]
        };
        context.ProductBillOfMaterials.Add(new ProductBillOfMaterial
        {
            Id = Guid.NewGuid(), Product = product, ProductId = productId, BillOfMaterial = bom,
            BillOfMaterialId = bom.Id, IsActive = true, EffectiveDate = Utc(27)
        });
        context.ProductPackages.Add(new ProductPackage
            { Id = Guid.NewGuid(), Product = product, ProductId = productId, Material = material, MaterialId = material.Id });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var complete = await Repository(context).GetProductionReadinessReport(Filter(departmentId), userId);
        Assert.True(complete.IsSuccess);
        Assert.Equal(1, complete.Value.ReadyProductCount);
        Assert.Equal(ProductReadiness.Ready, Assert.Single(complete.Value.Products).Configuration.Readiness);

        // A newer empty active BOM must not fall back to the older populated one.
        var emptyBom = new DOMAIN.Entities.BillOfMaterials.BillOfMaterial
            { Id = Guid.NewGuid(), ProductId = productId };
        context.ProductBillOfMaterials.Add(new ProductBillOfMaterial
        {
            Id = Guid.NewGuid(), ProductId = productId, BillOfMaterial = emptyBom,
            BillOfMaterialId = emptyBom.Id, IsActive = true, EffectiveDate = Utc(28)
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var incomplete = await Repository(context).GetProductionReadinessReport(Filter(departmentId), userId);
        Assert.Equal(DefinitionReadiness.Missing, Assert.Single(incomplete.Value.Products).Configuration.Bom);
        Assert.Equal(0, incomplete.Value.ReadyProductCount);
        var storedSpec = await context.ProductSpecifications.IgnoreAutoIncludes().SingleAsync(item => item.Id == specification.Id);
        storedSpec.DeletedAt = Utc(28);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var archived = await Repository(context).GetProductionReadinessReport(Filter(departmentId), userId);
        Assert.Equal(DefinitionReadiness.Missing, Assert.Single(archived.Value.Products).Configuration.Specification);
    }

    private static ApplicationDbContext Memory() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());
    private static ReportRepository Repository(ApplicationDbContext context) =>
        new(context, null!, null!, NullLogger<ReportRepository>.Instance);
    private static ProductionReadinessFilter Filter(Guid? department) =>
        new() { DepartmentId = department, StartDate = new(2026, 10, 28), EndDate = new(2026, 10, 28) };
    private static DateTime Utc(int day) => new(2026, 10, day, 0, 0, 0, DateTimeKind.Utc);
    private static Department Department(string name, DepartmentType type) =>
        new() { Id = Guid.NewGuid(), Name = name, Type = type };
    private static async Task<User> Seed(ApplicationDbContext context, bool granted = true,
        bool disabled = false, bool productionActor = false)
    {
        var department = Department("Production", DepartmentType.Production);
        var userDepartment = productionActor ? department : Department("QA", DepartmentType.NonProduction);
        context.Departments.AddRange(new[] { department, userDepartment }.Distinct());
        var user = new User { Id = Guid.NewGuid(), UserName = "Administrator", Department = userDepartment,
            DepartmentId = userDepartment.Id, IsDisabled = disabled };
        context.Users.Add(user);
        if (granted)
        {
            var role = new Role { Id = Guid.NewGuid(), Name = "Report reader" };
            context.Roles.Add(role);
            context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = role.Id });
            var claim = new IdentityRoleClaim<Guid> { RoleId = role.Id, ClaimType = AppConstants.Permission,
                ClaimValue = PermissionKeys.CanViewQcAndQaReport };
            context.RoleClaims.Add(claim);
            await context.SaveChangesAsync();
            context.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = claim.Id,
                Key = PermissionKeys.CanViewQcAndQaReport, Type = "View" });
        }
        await context.SaveChangesAsync();
        // Return the requested production scope separately from the authenticated user's QA department.
        return new User { Id = user.Id, DepartmentId = department.Id };
    }
    private static ProductionScheduleProduct AddBatch(ApplicationDbContext context, Guid? departmentId)
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "Product", Code = "P-1", DepartmentId = departmentId };
        var packing = new ProductPacking { Id = Guid.NewGuid(), Product = product, ProductId = product.Id,
            Name = "20 x 10", ExpectedYield = 1001, PackPerShipper = 300 };
        var schedule = new ProductionSchedule { Id = Guid.NewGuid(), Code = "PLAN", ScheduledStartTime = Utc(28),
            ScheduledEndTime = Utc(28).AddHours(23) };
        var item = new ProductionScheduleProduct { Id = Guid.NewGuid(), ProductionSchedule = schedule,
            ProductionScheduleId = schedule.Id, Product = product, ProductId = product.Id,
            ProductPacking = packing, ProductPackingId = packing.Id };
        schedule.Products.Add(item);
        product.Packings.Add(packing);
        context.ProductionSchedules.Add(schedule);
        return item;
    }
}

public sealed class LocalProductionReadinessFactAttribute : FactAttribute
{
    public LocalProductionReadinessFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORYX_READINESS_TEST_CONNECTION")))
            Skip = "Set ORYX_READINESS_TEST_CONNECTION to the dedicated disposable local PostgreSQL fixture.";
    }
}
