using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.BillOfMaterials.Request;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.ProductSpecifications;
using DOMAIN.Entities.ProductStandardTestProcedures;
using DOMAIN.Entities.QualityRoutines;
using DOMAIN.Entities.RndProjects;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>
/// A deleted product is history only. Every path that starts new work with a product refuses it, and
/// pickers never offer it. The caller is an R&amp;D user throughout: the product query filter does not
/// hide deleted products from R&amp;D or non-production departments, so these tests prove the explicit
/// guards rather than the filter.
/// </summary>
public class DeletedProductUsageTests
{
    private sealed class RndUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => nameof(SHARED.DepartmentType.RnD);
    }

    private sealed record Ids(Guid Active, Guid Deleted, Guid ActivePacking, Guid DeletedPacking, Guid Customer);

    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new RndUser());

    private static IMapper CreateMapper(ApplicationDbContext context = null)
    {
        var services = new ServiceCollection();
        if (context is not null) services.AddSingleton(context);
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddAutoMapper(_ => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static async Task<Ids> Seed(ApplicationDbContext context)
    {
        var ids = new Ids(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        context.AddRange(
            new Product { Id = ids.Active, Name = "Active Tablets", Code = "ACT-1" },
            new Product { Id = ids.Deleted, Name = "Withdrawn Syrup", Code = "WDR-1", DeletedAt = DateTime.UtcNow.AddDays(-1) },
            new ProductPacking { Id = ids.ActivePacking, ProductId = ids.Active, Name = "10 x 10" },
            new ProductPacking { Id = ids.DeletedPacking, ProductId = ids.Deleted, Name = "1 x 100ml" },
            new Customer { Id = ids.Customer, Name = "Hospital" });
        await context.SaveChangesAsync();
        return ids;
    }

    private static void AssertRefused(Result result, string productName)
    {
        Assert.True(result.IsFailure);
        var error = result.Error ?? result.Errors.Single();
        Assert.Equal(DeletedProductGuard.Code, error.Code);
        Assert.Contains(productName, error.Description);
    }

    [Fact]
    public async Task Guard_passes_active_products_and_names_every_deleted_one()
    {
        await using var context = CreateContext();
        var ids = await Seed(context);

        Assert.Null(await DeletedProductGuard.Refuse(context, ids.Active));
        Assert.Null(await DeletedProductGuard.Refuse(context, (Guid?)null));
        Assert.Null(await DeletedProductGuard.Refuse(context, Guid.NewGuid()));
        var refusal = await DeletedProductGuard.Refuse(context, [ids.Active, ids.Deleted]);
        Assert.NotNull(refusal);
        Assert.Equal(DeletedProductGuard.Code, refusal.Code);
        Assert.Contains("Withdrawn Syrup", refusal.Description);
        Assert.DoesNotContain("Active Tablets", refusal.Description);
    }

    [Fact]
    public async Task Production_schedule_refuses_a_deleted_product()
    {
        await using var context = CreateContext();
        var ids = await Seed(context);
        var repository = new ProductionScheduleRepository(context, CreateMapper(), null!, null!, null!, null!,
            new NoOpProductionActivityStepEventPublisher());

        var result = await repository.CreateProductionSchedule(new CreateProductionScheduleRequest
        {
            Code = "PS-1", ScheduledStartTime = DateTime.UtcNow.AddDays(1), ScheduledEndTime = DateTime.UtcNow.AddDays(2),
            Products = [new CreateProductionScheduleProduct { ProductId = ids.Deleted, ProductPackingId = ids.DeletedPacking, Quantity = 1 }],
        }, Guid.NewGuid());

        AssertRefused(result, "Withdrawn Syrup");
        Assert.Empty(context.ProductionSchedules.IgnoreQueryFilters());
    }

    [Fact]
    public async Task A_scheduled_batch_of_a_deleted_product_is_not_started()
    {
        await using var context = CreateContext();
        var ids = await Seed(context);
        var schedule = new ProductionSchedule { Id = Guid.NewGuid(), Code = "PS-OLD" };
        var batch = new ProductionScheduleProduct
        {
            Id = Guid.NewGuid(), ProductionScheduleId = schedule.Id, ProductId = ids.Deleted, ProductPackingId = ids.DeletedPacking,
        };
        context.AddRange(schedule, batch);
        await context.SaveChangesAsync();
        var repository = new ProductionScheduleRepository(context, CreateMapper(), null!, null!, null!, null!,
            new NoOpProductionActivityStepEventPublisher());

        var result = await repository.StartProductionActivity(batch.Id, Guid.NewGuid());

        AssertRefused(result, "Withdrawn Syrup");
        Assert.Empty(context.ProductionActivities.IgnoreQueryFilters());
    }

    [Fact]
    public async Task Bill_of_materials_refuses_a_deleted_product()
    {
        await using var context = CreateContext();
        var ids = await Seed(context);

        var result = await new BoMRepository(context, CreateMapper())
            .CreateBillOfMaterial(new CreateBillOfMaterialRequest { ProductId = ids.Deleted }, Guid.NewGuid());

        AssertRefused(result, "Withdrawn Syrup");
        Assert.Empty(context.BillOfMaterials.IgnoreQueryFilters());
    }

    [Fact]
    public async Task Product_master_sections_refuse_a_deleted_product()
    {
        await using var context = CreateContext();
        var ids = await Seed(context);
        var repository = new ProductRepository(context, CreateMapper());

        AssertRefused(await repository.UpdateProduct(new UpdateProductRequest(), ids.Deleted, Guid.NewGuid()), "Withdrawn Syrup");
        AssertRefused(await repository.CreateRoute([], ids.Deleted, Guid.NewGuid()), "Withdrawn Syrup");
        AssertRefused(await repository.CreateProductPacking([], ids.Deleted, Guid.NewGuid()), "Withdrawn Syrup");
        AssertRefused(await repository.CreateProductPackage([], ids.Deleted, Guid.NewGuid()), "Withdrawn Syrup");
    }

    [Fact]
    public async Task Specification_and_stp_refuse_a_deleted_product()
    {
        await using var context = CreateContext();
        var ids = await Seed(context);
        var mapper = CreateMapper();

        AssertRefused(await new ProductSpecificationRepository(context, mapper).CreateProductSpecification(
            new CreateProductSpecificationRequest { ProductIds = [ids.Active, ids.Deleted], DueDate = DateTime.UtcNow.AddDays(5) }),
            "Withdrawn Syrup");
        AssertRefused(await new ProductStandardTestProcedureRepository(context, mapper).CreateProductStandardTestProcedure(
            new CreateProductStandardTestProcedureRequest { ProductIds = [ids.Deleted], StpNumber = "STP-1" }),
            "Withdrawn Syrup");
        Assert.Empty(context.ProductSpecifications.IgnoreQueryFilters());
        Assert.Empty(context.ProductStandardTestProcedures.IgnoreQueryFilters());
    }

    [Fact]
    public async Task Microbial_requirement_refuses_a_deleted_product()
    {
        await using var context = CreateContext();
        var ids = await Seed(context);

        var result = await new MicrobialRequirementRepository(context).Create(new CreateMicrobialRequirementRequest
        {
            Subject = RequirementSubject.Product, ProductId = ids.Deleted, Stage = TestStage.Finished, Reason = "Liquid oral",
        }, Guid.NewGuid());

        AssertRefused(result, "Withdrawn Syrup");
    }

    [Fact]
    public async Task Rnd_project_refuses_a_deleted_product()
    {
        await using var context = CreateContext();
        var ids = await Seed(context);

        var result = await new RndProjectRepository(context, CreateMapper(), null!)
            .CreateProject(new CreateRndProjectRequest { ProductId = ids.Deleted, Title = "Reformulation" }, Guid.NewGuid());

        AssertRefused(result, "Withdrawn Syrup");
        Assert.Empty(context.RndProjects.IgnoreQueryFilters());
    }

    [Fact]
    public async Task Sales_documents_refuse_a_deleted_product()
    {
        await using var context = CreateContext();
        var ids = await Seed(context);
        var mapper = CreateMapper();

        var quotation = await new CustomerRepository(context, mapper, null!).CreateQuotation(ids.Customer,
            new CreateCustomerQuotationRequest
            {
                Code = "Q-1", ValidUntil = DateTime.UtcNow.AddDays(10),
                Items = [new CreateCustomerQuotationItemRequest { ProductId = ids.Deleted, ProductPackingId = ids.DeletedPacking, Quantity = 1, UnitPrice = 5 }],
            }, Guid.NewGuid());
        AssertRefused(quotation, "Withdrawn Syrup");

        var order = await new ProductionOrderRepository(context, mapper, null!).CreateProductionOrder(new CreateProductionOrderRequest
        {
            Code = "PO-1", CustomerId = ids.Customer,
            Products = [new CreateProductionOrderProduct { ProductId = ids.Deleted, ProductPackingId = ids.DeletedPacking, TotalOrderQuantity = 1 }],
        });
        AssertRefused(order, "Withdrawn Syrup");
        Assert.Empty(context.ProductionOrders.IgnoreQueryFilters());
    }

    [Fact]
    public async Task Product_picker_never_offers_a_deleted_product()
    {
        await using var context = CreateContext();
        await Seed(context);

        var page = await new ProductRepository(context, CreateMapper()).GetProducts(1, 50, null, null, null, null);

        Assert.True(page.IsSuccess);
        Assert.Equal(["Active Tablets"], page.Value.Data.Select(item => item.Name));
    }

    [Fact]
    public async Task Stp_pickers_never_offer_a_deleted_product()
    {
        await using var context = CreateContext();
        var ids = await Seed(context);
        context.AddRange(
            new ProductStandardTestProcedure { Id = Guid.NewGuid(), ProductId = ids.Active, StpNumber = "STP-A" },
            new ProductStandardTestProcedure { Id = Guid.NewGuid(), ProductId = ids.Deleted, StpNumber = "STP-D" });
        await context.SaveChangesAsync();
        // Products without an STP: add a second active product so the picker has something to show.
        context.Add(new Product { Id = Guid.NewGuid(), Name = "New Capsules", Code = "NEW-1" });
        context.Add(new Product { Id = Guid.NewGuid(), Name = "Retired Cream", Code = "RET-1", DeletedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
        var repository = new ProductStandardTestProcedureRepository(context, CreateMapper(context));

        var unused = await repository.GetProductsNotUsedInStandardTestProcedure(1, 50, null);
        Assert.Equal(["New Capsules"], unused.Value.Data.Select(item => item.Name));

        var forArd = await repository.GetProductStandardTestProceduresNotLinkedToArd(1, 50, null, TestStage.Finished, AnalysisType.Chemical);
        Assert.Equal(["STP-A"], forArd.Value.Data.Select(item => item.StpNumber));

        var all = await repository.GetProductStandardTestProcedures(1, 50, null);
        Assert.Equal(["STP-A"], all.Value.Data.Select(item => item.StpNumber));
    }
}
