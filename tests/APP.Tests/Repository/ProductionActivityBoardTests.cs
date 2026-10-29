using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.Packing;
using DOMAIN.Entities.Requisitions;
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
    public async Task Syrup_reference_values_are_preloaded_from_product_and_style_without_a_received_weight()
    {
        await using var fixture = await Fixture.Create();
        var run = fixture.Run("syrup-yield", fixture.Syrup, fixture.Secondary);
        fixture.Packing(run, "syrup");
        await fixture.Context.SaveChangesAsync();
        var result = await fixture.Repository.CreateFinalPacking(new CreateFinalPacking
        {
            ProductionScheduleProductId = run.ProductionScheduleProductId, ProductionActivityStepId = run.Steps[0].Id,
            AverageVolumeFilledPerBottle = 999, BatchSize = 1, TotalWeightReceived = 999,
            YieldTotalQuantityPacked = 20_025, QualityControlAnalyticalSample = 3, RetainedSamples = 12,
            TotalNumberOfBottles = 20_010, TotalQuantityPacked = 20_010, TotalGainOrLoss = 25,
            NumberOfBottlesPerShipper = 30, NUmberOfFullShipperPacked = 667,
        });
        Assert.True(result.IsSuccess);
        var stored = Assert.Single(await fixture.Context.FinalPackings.ToListAsync());
        Assert.Equal(20_000m, stored.ExpectedYield);
        Assert.Equal(3000m, stored.BatchSize);
        Assert.Equal(150m, stored.AverageVolumeFilledPerBottle);
        Assert.Equal(0m, stored.TotalWeightReceived);
        Assert.Equal(20_010m, stored.TotalQuantityPacked);
        Assert.Equal(25m, stored.TotalGainOrLoss);
        var read = await fixture.Repository.GetFinalPackingByScheduleProduct(run.ProductionScheduleProductId);
        Assert.True(read.IsSuccess);
        Assert.Equal(20_000m, read.Value.ExpectedYield);
    }

    [Theory]
    [InlineData("tube")]
    [InlineData("capsules")]
    [InlineData("tablet")]
    [InlineData("dry powder")]
    [InlineData("sachet")]
    public async Task Weight_based_packing_saves_expected_yield_using_kg_and_mg(string category)
    {
        await using var fixture = await Fixture.Create();
        var run = fixture.Run("weight-yield", fixture.Tablet, fixture.Secondary);
        fixture.Packing(run, category);
        await fixture.Context.SaveChangesAsync();
        var result = await fixture.Repository.CreateFinalPacking(new CreateFinalPacking
        {
            ProductionScheduleProductId = run.ProductionScheduleProductId,
            TotalWeightReceived = 120, AverageVolumeFilledPerBottle = 500,
        });
        Assert.True(result.IsSuccess);
        Assert.Equal(240_000m, Assert.Single(await fixture.Context.FinalPackings.ToListAsync()).ExpectedYield);
    }

    [Fact]
    public async Task Invalid_known_yield_measurements_do_not_save_or_complete_the_step()
    {
        await using var fixture = await Fixture.Create();
        var run = fixture.Run("invalid-yield", fixture.Syrup, fixture.Secondary);
        fixture.Packing(run, "syrup");
        fixture.Syrup.BaseQuantity = 0;
        await fixture.Context.SaveChangesAsync();
        var result = await fixture.Repository.CreateFinalPacking(new CreateFinalPacking
        {
            ProductionScheduleProductId = run.ProductionScheduleProductId, ProductionActivityStepId = run.Steps[0].Id,
            AverageVolumeFilledPerBottle = 0,
        });
        Assert.True(result.IsFailure);
        Assert.Empty(await fixture.Context.FinalPackings.ToListAsync());
        Assert.Equal(ProductionStatus.InProgress, run.Steps[0].Status);
    }

    [Fact]
    public async Task Syrup_uses_full_product_batch_and_style_yield_and_invalid_setup_preserves_the_record()
    {
        await using var fixture = await Fixture.Create();
        var run = fixture.Run("updated-yield", fixture.Syrup, fixture.Secondary);
        fixture.Packing(run, "Bottle");
        run.ProductionScheduleProduct.ProductPacking.ExpectedYield = 20_000;
        run.ProductionScheduleProduct.Quantity = 1_500_000;
        await fixture.Context.SaveChangesAsync();
        var request = new CreateFinalPacking { ProductionScheduleProductId = run.ProductionScheduleProductId, AverageVolumeFilledPerBottle = 75 };
        var created = await fixture.Repository.CreateFinalPacking(request);
        Assert.True(created.IsSuccess);
        var stored = Assert.Single(await fixture.Context.FinalPackings.ToListAsync());
        Assert.Equal(20_000m, stored.ExpectedYield);
        Assert.Equal(3000m, stored.BatchSize);
        Assert.Equal(150m, stored.AverageVolumeFilledPerBottle);
        run.ProductionScheduleProduct.ProductPacking.ExpectedYield = 19_980;
        await fixture.Context.SaveChangesAsync();
        Assert.True((await fixture.Repository.UpdateFinalPacking(request, created.Value)).IsSuccess);
        Assert.Equal(19_980m, stored.ExpectedYield);
        fixture.Syrup.BaseQuantity = 0;
        await fixture.Context.SaveChangesAsync();
        Assert.True((await fixture.Repository.UpdateFinalPacking(request, created.Value)).IsFailure);
        Assert.Equal(19_980m, stored.ExpectedYield);
    }

    [Fact]
    public async Task Missing_final_packing_returns_not_found_instead_of_a_null_validation_error()
    {
        await using var fixture = await Fixture.Create();
        var byId = await fixture.Repository.GetFinalPacking(Guid.NewGuid());
        var byBatch = await fixture.Repository.GetFinalPackingByScheduleProduct(Guid.NewGuid());
        foreach (var result in new[] { byId, byBatch })
        {
            Assert.True(result.IsFailure);
            Assert.Equal("FinalPacking.NotFound",result.Error.Code);
            Assert.Equal(ErrorType.NotFound,result.Error.Type);
        }
    }

    [Theory]
    [InlineData(ProductionExtraPackingStatus.Pending)]
    [InlineData(ProductionExtraPackingStatus.InProgress)]
    public async Task Extra_material_blocks_completion_and_final_packing_until_issued(ProductionExtraPackingStatus status)
    {
        await using var fixture = await Fixture.Create();
        var run = fixture.Run("outstanding-extra", fixture.Syrup, fixture.Secondary);
        var step = run.Steps[0];
        step.IsCritical = false;
        fixture.Extra(run, ProductionExtraPackingStatus.Approved);
        fixture.Extra(run, status);
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Repository.UpdateStatusOfProductionActivityStep(step.Id, ProductionStatus.Completed, Guid.NewGuid());
        var packing = await fixture.Repository.CreateFinalPacking(new CreateFinalPacking
        {
            ProductionScheduleProductId = run.ProductionScheduleProductId, ProductionActivityStepId = step.Id
        });

        Assert.True(result.IsFailure);
        Assert.True(packing.IsFailure);
        Assert.Equal(ProductionStatus.InProgress, step.Status);
        Assert.Null(step.CompletedAt);
        Assert.Null(run.CompletedAt);
        Assert.Empty(run.ActivityLogs);
        Assert.Empty(await fixture.Context.FinalPackings.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Completion_proceeds_with_no_extra_material_or_only_issued_material(bool hasExtra)
    {
        await using var fixture = await Fixture.Create();
        var run = fixture.Run("issued-extra", fixture.Syrup, fixture.Secondary);
        var step = run.Steps[0];
        step.IsCritical = false;
        if (hasExtra)
        {
            fixture.Extra(run, ProductionExtraPackingStatus.Approved);
            fixture.Extra(run, ProductionExtraPackingStatus.Approved);
        }
        // Outstanding material belonging to a different batch does not block this batch.
        fixture.Extra(fixture.Run("another-batch", fixture.Syrup, fixture.Secondary), ProductionExtraPackingStatus.Pending);
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Repository.UpdateStatusOfProductionActivityStep(step.Id, ProductionStatus.Completed, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(ProductionStatus.Completed, step.Status);
        Assert.NotNull(step.CompletedAt);
        Assert.Single(run.ActivityLogs);
    }

    [Fact]
    public async Task The_step_before_final_packing_waits_until_packaging_is_requested_and_issued()
    {
        await using var fixture = await Fixture.Create();
        var run = fixture.Run("no-packaging", fixture.Syrup, fixture.Secondary, packagingRequested: false);
        var step = run.Steps[0];
        step.IsCritical = false;
        fixture.FinalPackingStep(run);
        await fixture.Context.SaveChangesAsync();

        var notRequested = await fixture.Repository.UpdateStatusOfProductionActivityStep(step.Id, ProductionStatus.Completed, Guid.NewGuid());
        Assert.Equal("ProductActivity.PackagingNotIssued", notRequested.Errors.Single().Code);

        var requisition = fixture.PackagingRequisition(run.ProductionScheduleProductId, issued: false);
        await fixture.Context.SaveChangesAsync();
        var notIssued = await fixture.Repository.UpdateStatusOfProductionActivityStep(step.Id, ProductionStatus.Completed, Guid.NewGuid());
        Assert.Equal("ProductActivity.PackagingNotIssued", notIssued.Errors.Single().Code);
        Assert.Equal(ProductionStatus.InProgress, step.Status);
        Assert.Null(step.CompletedAt);

        requisition.Status = RequestStatus.Completed;
        await fixture.Context.SaveChangesAsync();
        var completed = await fixture.Repository.UpdateStatusOfProductionActivityStep(step.Id, ProductionStatus.Completed, Guid.NewGuid());
        Assert.True(completed.IsSuccess);
        Assert.Equal(ProductionStatus.Completed, step.Status);
    }

    [Fact]
    public async Task A_step_packaging_was_requested_from_is_completed_by_hand_once_it_is_issued()
    {
        await using var fixture = await Fixture.Create();
        var run = fixture.Run("requested-here", fixture.Syrup, fixture.Secondary, packagingRequested: false);
        var step = run.Steps[0];
        step.IsCritical = false;
        var requisition = fixture.PackagingRequisition(run.ProductionScheduleProductId, issued: false, stepId: step.Id);
        await fixture.Context.SaveChangesAsync();

        var pending = await fixture.Repository.UpdateStatusOfProductionActivityStep(step.Id, ProductionStatus.Completed, Guid.NewGuid());
        Assert.Equal("ProductActivity.PackagingNotIssued", pending.Errors.Single().Code);
        Assert.Equal(ProductionStatus.InProgress, step.Status);

        requisition.Status = RequestStatus.Completed;
        await fixture.Context.SaveChangesAsync();
        Assert.True((await fixture.Repository.UpdateStatusOfProductionActivityStep(step.Id, ProductionStatus.Completed, Guid.NewGuid())).IsSuccess);
        Assert.Equal(ProductionStatus.Completed, step.Status);
    }

    [Fact]
    public async Task Final_packing_is_refused_until_packaging_is_issued()
    {
        await using var fixture = await Fixture.Create();
        var run = fixture.Run("not-issued", fixture.Syrup, fixture.Secondary, packagingRequested: false);
        fixture.PackagingRequisition(run.ProductionScheduleProductId, issued: false);
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Repository.CreateFinalPacking(new CreateFinalPacking
        {
            ProductionScheduleProductId = run.ProductionScheduleProductId, ProductionActivityStepId = run.Steps[0].Id
        });

        Assert.Equal("FinalPacking.PackagingNotIssued", result.Errors.Single().Code);
        Assert.Empty(await fixture.Context.FinalPackings.ToListAsync());
    }

    [Fact]
    public async Task Pending_extra_material_also_blocks_a_direct_start_without_changing_the_step()
    {
        await using var fixture = await Fixture.Create();
        var run = fixture.Run("pending-extra", fixture.Syrup, fixture.Secondary);
        var step = run.Steps[0];
        step.IsCritical = false;
        step.Status = ProductionStatus.New;
        fixture.Extra(run, ProductionExtraPackingStatus.Pending);
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Repository.UpdateStatusOfProductionActivityStep(step.Id, ProductionStatus.InProgress, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ProductionStatus.New, step.Status);
        Assert.Null(step.StartedAt);
        Assert.Empty(run.ActivityLogs);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Final_packing_can_complete_after_all_extra_material_has_been_issued(bool hasExtra)
    {
        await using var fixture = await Fixture.Create();
        var run = fixture.Run("packing-issued", fixture.Syrup, fixture.Secondary);
        fixture.Packing(run, "syrup");
        if (hasExtra) fixture.Extra(run, ProductionExtraPackingStatus.Approved);
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Repository.CreateFinalPacking(new CreateFinalPacking
        {
            ProductionScheduleProductId = run.ProductionScheduleProductId, ProductionActivityStepId = run.Steps[0].Id
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(ProductionStatus.Completed, run.Steps[0].Status);
        Assert.Single(await fixture.Context.FinalPackings.ToListAsync());
    }

    [Fact]
    public async Task A_different_batch_cannot_be_used_to_bypass_the_packing_issue_gate()
    {
        await using var fixture = await Fixture.Create();
        var run = fixture.Run("packing-unissued", fixture.Syrup, fixture.Secondary);
        fixture.Extra(run, ProductionExtraPackingStatus.InProgress);
        var other = fixture.Run("different-batch", fixture.Syrup, fixture.Secondary);
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Repository.CreateFinalPacking(new CreateFinalPacking
        {
            ProductionScheduleProductId = other.ProductionScheduleProductId, ProductionActivityStepId = run.Steps[0].Id
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ProductionStatus.InProgress, run.Steps[0].Status);
        Assert.Null(run.Steps[0].CompletedAt);
        Assert.Empty(await fixture.Context.FinalPackings.ToListAsync());
    }

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
        public ProductionScheduleRepository Repository => new(Context, Services.GetRequiredService<IMapper>(), null!, null!, new NoOpBackgroundWorker(), null!, new NoOpProductionActivityStepEventPublisher());

        private Fixture(bool productionUser)
        {
            Context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
                new BoardUser(SyrupDepartment.Id, productionUser ? nameof(DepartmentType.Production) : nameof(DepartmentType.NonProduction)));
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(Context);
            var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
            http.HttpContext.Request.Host = new HostString("oryx.test");
            services.AddSingleton<IHttpContextAccessor>(http);
            services.AddAutoMapper(configuration => { }, typeof(OryxMapper));
            Services = services.BuildServiceProvider();
            var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Millilitre", Symbol = "ml" };
            Syrup = new Product { Id = Guid.NewGuid(), Name = "Coldrilif Syrup", Code = "NL-020", FullBatchSize = 3_000_000, BaseQuantity = 150, DepartmentId = SyrupDepartment.Id, Department = SyrupDepartment, BaseUomId = unit.Id, BaseUoM = unit };
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

        public ProductionActivity Run(string? batchNumber, Product product, Operation operation, bool packagingRequested = true)
        {
            var batch = new ProductionScheduleProduct { Id = Guid.NewGuid(), ProductionScheduleId = Schedule.Id, ProductionSchedule = Schedule,
                ProductId = product.Id, Product = product, BatchNumber = batchNumber!, Quantity = 3_000_000 };
            if (packagingRequested) PackagingRequisition(batch.Id);
            var run = new ProductionActivity { Id = Guid.NewGuid(), Code = Guid.NewGuid().ToString(), ProductionScheduleProductId = batch.Id,
                ProductionScheduleProduct = batch, StartedAt = DateTime.UtcNow };
            run.Steps.Add(new ProductionActivityStep { Id = Guid.NewGuid(), ProductionActivityId = run.Id, ProductionActivity = run,
                OperationId = operation.Id, Operation = operation, Order = 2, Status = ProductionStatus.InProgress, IsCritical = true });
            Context.ProductionActivities.Add(run);
            return run;
        }

        public Requisition PackagingRequisition(Guid batchId, bool issued = true, Guid? stepId = null)
        {
            var requisition = new Requisition
            {
                Id = Guid.NewGuid(), Code = $"PM/N/26/{Guid.NewGuid():N}", RequisitionType = RequisitionType.Stock, ProductionScheduleProductId = batchId,
                ProductionActivityStepId = stepId, Status = issued ? RequestStatus.Completed : RequestStatus.Pending,
            };
            Context.Requisitions.Add(requisition);
            return requisition;
        }

        /// <summary>Adds a final packing step right after the run's first step.</summary>
        public ProductionActivityStep FinalPackingStep(ProductionActivity run)
        {
            var operation = new Operation { Id = Guid.NewGuid(), Name = "Final Packing" };
            var step = new ProductionActivityStep { Id = Guid.NewGuid(), ProductionActivityId = run.Id, ProductionActivity = run,
                OperationId = operation.Id, Operation = operation, Order = run.Steps[0].Order + 1, Status = ProductionStatus.New };
            run.Steps.Add(step);
            Context.Add(operation);
            return step;
        }

        public void Extra(ProductionActivity run, ProductionExtraPackingStatus status)
        {
            var material = new Material { Id = Guid.NewGuid(), Name = "Extra material", Code = Guid.NewGuid().ToString() };
            Context.ProductionExtraPackings.Add(new ProductionExtraPacking
            {
                Id = Guid.NewGuid(), ProductionScheduleProductId = run.ProductionScheduleProductId,
                ProductionScheduleProduct = run.ProductionScheduleProduct, Material = material, MaterialId = material.Id,
                UoMId = Syrup.BaseUoM.Id, UoM = Syrup.BaseUoM, Quantity = 10, Status = status,
                Approved = status != ProductionExtraPackingStatus.Pending,
                IssuedAt = status == ProductionExtraPackingStatus.Approved ? DateTime.UtcNow : null
            });
        }

        public void Packing(ProductionActivity run, string category)
        {
            var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Name = category, Symbol = category };
            var packing = new ProductPacking { Id = Guid.NewGuid(), Name = "Yield test style", Product = run.ProductionScheduleProduct.Product,
                ProductId = run.ProductionScheduleProduct.ProductId, BasePackingUoM = unit, BasePackingUomId = unit.Id, ExpectedYield = category == "syrup" ? 20_000 : 1 };
            run.ProductionScheduleProduct.ProductPacking = packing;
            run.ProductionScheduleProduct.ProductPackingId = packing.Id;
            Context.AddRange(unit, packing);
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

file sealed class NoOpBackgroundWorker : APP.Services.Background.IBackgroundWorkerService
{
    public void EnqueueLog(DOMAIN.Entities.ActivityLogs.CreateActivityLog log) { }
    public void EnqueuePrevStateCapture(DOMAIN.Entities.ActivityLogs.PrevStateCaptureRequest prevStateCaptureRequest) { }
    public void EnqueueNotification(string message, DOMAIN.Entities.Notifications.NotificationType type, Guid? departmentId = null,
        List<User> users = null!, Guid? shipmentDocumentId = null, string relatedEntityType = null!, Guid? relatedEntityId = null, string relatedEntityCode = null!,
        string relatedEntityName = null!, string relatedEntityStatus = null!, string relatedEntityPriority = null!) { }
    public Task DoWork(CancellationToken stoppingToken) => Task.CompletedTask;
}
