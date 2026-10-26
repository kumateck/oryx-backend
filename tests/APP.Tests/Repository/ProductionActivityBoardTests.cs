using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Routes;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class ProductionActivityBoardTests
{
    [Fact]
    public async Task Includes_running_syrup_batches_whose_operations_are_outside_the_department_catalog()
    {
        await using var fixture = await Fixture.Create();
        var secondary = fixture.Run("4862", fixture.Syrup, fixture.Secondary);
        var sampling = fixture.Run("1004", fixture.Syrup, fixture.Sampling);
        var regular = fixture.Run("1003", fixture.Syrup, fixture.Configured);
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Repository.GetProductionActivityGroupedByOperation(fixture.SyrupDepartment.Id);

        Assert.True(result.IsSuccess);
        var batches = result.Value.SelectMany(column => column.Activities).ToList();
        Assert.Equal(3, batches.Count);
        Assert.Contains(batches, batch => batch.Id == secondary.Id && batch.BatchNumber == "4862");
        Assert.Contains(batches, batch => batch.Id == sampling.Id && batch.BatchNumber == "1004");
        Assert.Contains(batches, batch => batch.Id == regular.Id);
        Assert.Equal(secondary.Id, Assert.Single(result.Value.Single(column => column.Operation.Id == fixture.Secondary.Id).Activities).Id);
        Assert.Equal(sampling.Id, Assert.Single(result.Value.Single(column => column.Operation.Id == fixture.Sampling.Id).Activities).Id);
    }

    [Fact]
    public async Task Scopes_batches_by_product_department_even_when_other_products_use_the_same_operation()
    {
        await using var fixture = await Fixture.Create();
        var syrup = fixture.Run("S-1", fixture.Syrup, fixture.Secondary);
        fixture.Run("T-1", fixture.Tablet, fixture.Secondary);
        fixture.Run("T-2", fixture.Tablet, fixture.Configured);
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Repository.GetProductionActivityGroupedByOperation(fixture.SyrupDepartment.Id);

        Assert.Equal(syrup.Id, Assert.Single(result.Value.SelectMany(column => column.Activities)).Id);
    }

    [Fact]
    public async Task Retains_empty_configured_columns_and_combines_multiple_batches_at_the_same_operation_once()
    {
        await using var fixture = await Fixture.Create();
        fixture.Run("S-1", fixture.Syrup, fixture.Secondary);
        fixture.Run("S-2", fixture.Syrup, fixture.Secondary);
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Repository.GetProductionActivityGroupedByOperation(fixture.SyrupDepartment.Id);

        Assert.Empty(result.Value.Single(column => column.Operation.Id == fixture.Configured.Id).Activities);
        Assert.Equal(2, result.Value.Single(column => column.Operation.Id == fixture.Secondary.Id).Activities.Count);
        Assert.Equal(result.Value.Count, result.Value.Select(column => column.Operation.Id).Distinct().Count());
        Assert.Equal(result.Value.OrderBy(column => column.Operation.Order).Select(column => column.Operation.Id), result.Value.Select(column => column.Operation.Id));
    }

    [Fact]
    public async Task Excludes_cancelled_completed_and_fully_finished_runs()
    {
        await using var fixture = await Fixture.Create();
        var active = fixture.Run("active", fixture.Syrup, fixture.Secondary);
        fixture.Run("cancelled batch", fixture.Syrup, fixture.Secondary).ProductionScheduleProduct.Cancelled = true;
        fixture.Run("cancelled activity", fixture.Syrup, fixture.Secondary).Status = ProductionStatus.Cancelled;
        fixture.Run("completed status", fixture.Syrup, fixture.Secondary).Status = ProductionStatus.Completed;
        fixture.Run("completed timestamp", fixture.Syrup, fixture.Secondary).CompletedAt = DateTime.UtcNow;
        var finished = fixture.Run("all steps done", fixture.Syrup, fixture.Secondary);
        finished.Steps.ForEach(step => step.CompletedAt = DateTime.UtcNow);
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Repository.GetProductionActivityGroupedByOperation(fixture.SyrupDepartment.Id);

        Assert.Equal(active.Id, Assert.Single(result.Value.SelectMany(column => column.Activities)).Id);
    }

    [Fact]
    public async Task Reads_only_the_current_step_and_preserves_the_fields_the_board_uses()
    {
        await using var fixture = await Fixture.Create();
        var run = fixture.Run("4862", fixture.Syrup, fixture.Secondary);
        var assigned = new User { Id = Guid.NewGuid(), FirstName = "Ada", LastName = "Mensah", Avatar = "ada.png" };
        var previous = new ProductionActivityStep { Id = Guid.NewGuid(), ProductionActivity = run, ProductionActivityId = run.Id,
            Operation = fixture.Configured, OperationId = fixture.Configured.Id, Order = 1, Status = ProductionStatus.Completed, CompletedAt = DateTime.UtcNow };
        run.Steps.Add(previous);
        run.Steps[0].ResponsibleUsers.Add(new ProductionActivityStepUser { Id = Guid.NewGuid(), User = assigned, UserId = assigned.Id, Action = OperationAction.FinalPackingAndPartialReturn });
        var later = new ProductionActivityStep { Id = Guid.NewGuid(), ProductionActivity = run, ProductionActivityId = run.Id,
            Operation = fixture.Sampling, OperationId = fixture.Sampling.Id, Order = 3 };
        run.Steps.Add(later);
        fixture.Context.Users.Add(assigned);
        await fixture.Context.SaveChangesAsync();
        fixture.Context.ChangeTracker.Clear();
        fixture.SetRequestContext();

        var projected = Assert.Single(await ProductionActivityBoardQuery.ForDepartment(fixture.Context, fixture.SyrupDepartment.Id).ToListAsync());
        Assert.Equal(fixture.Secondary.Id, projected.CurrentStep.OperationId);
        Assert.Single(projected.CurrentStep.ResponsibleUsers);
        Assert.Empty(fixture.Context.ChangeTracker.Entries());
        var result = await fixture.Repository.GetProductionActivityGroupedByOperation(fixture.SyrupDepartment.Id);
        var batch = Assert.Single(result.Value.SelectMany(column => column.Activities));
        Assert.Equal(run.Id, batch.CurrentStep.ProductionActivity.Id);
        Assert.Equal("Coldrilif Syrup", batch.ProductionScheduleProduct.Product.Name);
        Assert.Equal("ml", batch.ProductionScheduleProduct.Product.BaseUoM.Symbol);
        Assert.Equal("PS031", batch.ProductionScheduleProduct.ProductionSchedule.Code);
        var person = Assert.Single(batch.CurrentStep.ResponsibleUsers);
        Assert.Equal("Ada", person.User.FirstName);
        Assert.Equal("http://oryx.test/api/v1/file/avatar/ada.png", person.User.Avatar);
        Assert.Equal(OperationAction.FinalPackingAndPartialReturn, person.Action);
    }

    [Fact]
    public async Task Resolves_missing_batch_numbers_from_the_bmr_without_the_batch_mapper()
    {
        await using var fixture = await Fixture.Create();
        var run = fixture.Run(null, fixture.Syrup, fixture.Secondary);
        fixture.Context.BatchManufacturingRecords.Add(new BatchManufacturingRecord { Id = Guid.NewGuid(),
            ProductionScheduleProductId = run.ProductionScheduleProductId, ProductionScheduleProduct = run.ProductionScheduleProduct,
            ProductionActivityStepId = run.Steps[0].Id, ProductionActivityStep = run.Steps[0], BatchNumber = "BMR-4862" });
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Repository.GetProductionActivityGroupedByOperation(fixture.SyrupDepartment.Id);

        var batch = Assert.Single(result.Value.SelectMany(column => column.Activities));
        Assert.Equal("BMR-4862", batch.BatchNumber);
        Assert.Equal("BMR-4862", batch.ProductionScheduleProduct.BatchNumber);
    }

    [Fact]
    public async Task Missing_department_parameter_does_not_bypass_existing_product_visibility()
    {
        await using var fixture = await Fixture.Create(productionUser: true);
        var syrup = fixture.Run("S-1", fixture.Syrup, fixture.Secondary);
        fixture.Run("T-1", fixture.Tablet, fixture.Secondary);
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Repository.GetProductionActivityGroupedByOperation(null);

        Assert.Equal(syrup.Id, Assert.Single(result.Value.SelectMany(column => column.Activities)).Id);
    }

    [Fact]
    public void Board_projection_translates_on_postgresql_without_loading_full_user_or_step_details()
    {
        using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=BoardTranslationOnly;Username=unused").Options,
            new BoardUser(Guid.NewGuid(), nameof(DepartmentType.NonProduction)));

        // ToQueryString compiles the real provider query without connecting to a database.
        var sql = ProductionActivityBoardQuery.ForDepartment(context, Guid.NewGuid()).ToQueryString();
        Assert.Contains("ProductionActivities", sql);
        Assert.DoesNotContain("PasswordHash", sql);
        Assert.DoesNotContain("Signature", sql);
        Assert.DoesNotContain("ProductionActivityStepResources", sql);
        Assert.DoesNotContain("ProductionActivityLogs", sql);
    }

    private sealed record BoardUser(Guid Department, string DepartmentType) : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => Department;
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public ApplicationDbContext Context { get; }
        private ServiceProvider Services { get; }
        public Department SyrupDepartment { get; } = new() { Id = Guid.NewGuid(), Name = "Syrup" };
        private Department OtherDepartment { get; } = new() { Id = Guid.NewGuid(), Name = "Other production" };
        public Product Syrup { get; }
        public Product Tablet { get; }
        public Operation Configured { get; }
        public Operation Secondary { get; }
        public Operation Sampling { get; }
        private ProductionSchedule Schedule { get; }
        public ProductionScheduleRepository Repository => new(Context, Services.GetRequiredService<IMapper>(), null!, null!, null!, null!, new NoOpProductionActivityStepEventPublisher());

        private Fixture(bool productionUser)
        {
            Context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
                new BoardUser(SyrupDepartment.Id, productionUser ? nameof(DepartmentType.Production) : nameof(DepartmentType.NonProduction)));
            var services = new ServiceCollection();
            services.AddLogging();
            var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
            http.HttpContext.Request.Host = new HostString("oryx.test");
            services.AddSingleton<IHttpContextAccessor>(http);
            services.AddAutoMapper(configuration => { }, typeof(OryxMapper));
            Services = services.BuildServiceProvider();
            var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Millilitre", Symbol = "ml" };
            Syrup = new Product { Id = Guid.NewGuid(), Name = "Coldrilif Syrup", Code = "NL-020", DepartmentId = SyrupDepartment.Id, Department = SyrupDepartment, BaseUomId = unit.Id, BaseUoM = unit };
            Tablet = new Product { Id = Guid.NewGuid(), Name = "Other product", Code = "OTHER", DepartmentId = OtherDepartment.Id, Department = OtherDepartment };
            Configured = new Operation { Id = Guid.NewGuid(), Name = "Configured syrup operation", DepartmentId = SyrupDepartment.Id, Order = 1 };
            Secondary = new Operation { Id = Guid.NewGuid(), Name = "Secondary packing activity", DepartmentId = OtherDepartment.Id, Order = 11 };
            Sampling = new Operation { Id = Guid.NewGuid(), Name = "Intermediate granules sampling and release", DepartmentId = null, Order = 6 };
            Schedule = new ProductionSchedule { Id = Guid.NewGuid(), Code = "PS031", DepartmentId = SyrupDepartment.Id, Department = SyrupDepartment };
            Context.AddRange(SyrupDepartment, OtherDepartment, Syrup, Tablet, unit, Configured, Secondary, Sampling, Schedule);
        }

        public static async Task<Fixture> Create(bool productionUser = false)
        {
            var fixture = new Fixture(productionUser);
            await fixture.Context.SaveChangesAsync();
            return fixture;
        }

        public ProductionActivity Run(string? batchNumber, Product product, Operation operation)
        {
            var batch = new ProductionScheduleProduct { Id = Guid.NewGuid(), ProductionScheduleId = Schedule.Id, ProductionSchedule = Schedule,
                ProductId = product.Id, Product = product, BatchNumber = batchNumber!, Quantity = 3_000_000 };
            var run = new ProductionActivity { Id = Guid.NewGuid(), Code = Guid.NewGuid().ToString(), ProductionScheduleProductId = batch.Id,
                ProductionScheduleProduct = batch, StartedAt = DateTime.UtcNow };
            run.Steps.Add(new ProductionActivityStep { Id = Guid.NewGuid(), ProductionActivityId = run.Id, ProductionActivity = run,
                OperationId = operation.Id, Operation = operation, Order = 2, Status = ProductionStatus.InProgress, IsCritical = true });
            Context.ProductionActivities.Add(run);
            return run;
        }

        public void SetRequestContext()
        {
            var http = new DefaultHttpContext();
            http.Request.Host = new HostString("oryx.test");
            Services.GetRequiredService<IHttpContextAccessor>().HttpContext = http;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Services.DisposeAsync();
        }
    }
}
