using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class GuardCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

/// <summary>
/// No path may store a price that cannot be costed: EPRC-69 carried "1.2" with no unit and was
/// costed as 1.2 x 650,000,000 mg = 780,000,000. Each write path refuses a blank or unrelated
/// price unit, checking refuses an order that still has one, and the save guard catches any
/// path that forgets - without failing unrelated changes to legacy rows.
/// </summary>
public class PriceUoMGuardTests
{
    [Theory]
    [InlineData("mg", "kg", true)]
    [InlineData("kg", "g", true)]
    [InlineData("Pcs", "pcs", true)]
    [InlineData("mg", "", false)]
    [InlineData("mg", "  ", false)]
    [InlineData("mg", null, false)]
    [InlineData("mg", "L", false)]
    [InlineData("Pcs", "kg", false)]
    public void CanPrice_needs_a_unit_that_converts(string quantityUoM, string priceUoM, bool expected) =>
        Assert.Equal(expected, UomConverter.CanPrice(quantityUoM, priceUoM));

    [Theory]
    [InlineData("")]
    [InlineData("L")]
    public async Task Quotation_response_is_refused_without_a_usable_unit(string priceUoM)
    {
        await using var context = CreateContext();
        var seed = await SeedQuotationAsync(context);

        var result = await CreateRequisitions(context).ReceiveQuotationFromSupplier(
            [new SupplierQuotationResponseDto { Id = seed.NewItemId, Price = 1.2m, PriceUoM = priceUoM }],
            seed.QuotationId);

        Assert.True(result.IsFailure);
        Assert.Equal(PriceUoMErrors.Code, result.Error.Code);
        var item = await context.SupplierQuotationItems.FirstAsync(i => i.Id == seed.NewItemId);
        Assert.Null(item.QuotedPrice);
    }

    [Fact]
    public async Task Quotation_response_saves_next_to_an_untouched_legacy_line()
    {
        await using var context = CreateContext();
        var seed = await SeedQuotationAsync(context);

        var result = await CreateRequisitions(context).ReceiveQuotationFromSupplier(
            [new SupplierQuotationResponseDto { Id = seed.NewItemId, Price = 1.2m, PriceUoM = " kg " }],
            seed.QuotationId);

        Assert.True(result.IsSuccess);
        var item = await context.SupplierQuotationItems.FirstAsync(i => i.Id == seed.NewItemId);
        Assert.Equal("kg", item.PriceUoM);
    }

    [Fact]
    public async Task Creating_an_order_line_without_a_unit_is_refused()
    {
        await using var context = CreateContext();
        var seed = await SeedOrderAsync(context, linePriceUoM: "kg");

        var result = await CreateProcurement(context).CreatePurchaseOrder(new CreatePurchaseOrderRequest
        {
            Code = "EPRC-NEW",
            SupplierId = seed.SupplierId,
            SourceRequisitionId = Guid.NewGuid(),
            Items = [new CreatePurchaseOrderItemRequest { MaterialId = seed.MaterialId, UomId = seed.MgId, Quantity = 1, Price = 1.2m, PriceUoM = "" }],
        }, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(PriceUoMErrors.Code, result.Error.Code);
        Assert.Single(context.PurchaseOrders);
    }

    [Fact]
    public async Task Revising_a_unitless_line_without_giving_a_unit_is_refused()
    {
        await using var context = CreateContext();
        var seed = await SeedOrderAsync(context, linePriceUoM: "");

        var result = await CreateProcurement(context).RevisePurchaseOrder(seed.OrderId,
        [
            new CreatePurchaseOrderRevision
            {
                Type = RevisedPurchaseOrderType.UpdateItem,
                PurchaseOrderItemId = seed.LineId,
                UoMId = seed.MgId,
                Quantity = 650_000_000m,
                Price = 1.3m,
            },
        ]);

        Assert.True(result.IsFailure);
        Assert.Equal(PriceUoMErrors.Code, result.Error.Code);
        Assert.Equal(1.2m, (await context.PurchaseOrderItems.FirstAsync()).Price);
    }

    [Fact]
    public async Task Adding_a_line_priced_in_an_unrelated_unit_is_refused()
    {
        await using var context = CreateContext();
        var seed = await SeedOrderAsync(context, linePriceUoM: "kg");

        var result = await CreateProcurement(context).RevisePurchaseOrder(seed.OrderId,
        [
            new CreatePurchaseOrderRevision
            {
                Type = RevisedPurchaseOrderType.AddItem,
                MaterialId = seed.MaterialId,
                UoMId = seed.MgId,
                Quantity = 1m,
                Price = 1m,
                PriceUoM = "L",
                CurrencyId = Guid.NewGuid(),
            },
        ]);

        Assert.True(result.IsFailure);
        Assert.Equal(PriceUoMErrors.Code, result.Error.Code);
        Assert.Single(context.PurchaseOrderItems);
    }

    [Fact]
    public async Task Checking_an_order_with_a_unitless_line_is_refused()
    {
        await using var context = CreateContext();
        var seed = await SeedOrderAsync(context, linePriceUoM: "");

        var result = await CreateProcurement(context).CheckPurchaseOrder(seed.OrderId);

        Assert.True(result.IsFailure);
        Assert.Equal(PriceUoMErrors.Code, result.Error.Code);
        Assert.Equal(PurchaseOrderStatus.Revised, (await context.PurchaseOrders.FirstAsync()).Status);
    }

    [Fact]
    public async Task Checking_takes_the_awarded_quotations_unit_and_keeps_it()
    {
        await using var context = CreateContext();
        var seed = await SeedOrderAsync(context, linePriceUoM: "");
        var quotation = new SupplierQuotation { Id = Guid.NewGuid(), SupplierId = seed.SupplierId };
        context.SupplierQuotations.Add(quotation);
        context.SupplierQuotationItems.Add(new SupplierQuotationItem
        {
            Id = Guid.NewGuid(),
            SupplierQuotationId = quotation.Id,
            MaterialId = seed.MaterialId,
            UoMId = seed.MgId,
            QuotedPrice = 1.2m,
            PriceUoM = "kg",
            Status = SupplierQuotationItemStatus.Processed,
            PurchaseOrderId = seed.OrderId,
        });
        await context.SaveChangesAsync();

        var result = await CreateProcurement(context).CheckPurchaseOrder(seed.OrderId);

        Assert.True(result.IsSuccess);
        Assert.Equal("kg", (await context.PurchaseOrderItems.FirstAsync()).PriceUoM);
        // No approval workflow is seeded, so the checked order is approved automatically.
        Assert.Equal(PurchaseOrderStatus.Approved, (await context.PurchaseOrders.FirstAsync()).Status);
    }

    [Fact]
    public async Task Save_guard_refuses_writing_a_price_without_a_unit()
    {
        await using var context = CreateContext();
        var seed = await SeedOrderAsync(context, linePriceUoM: "kg");
        var line = await context.PurchaseOrderItems.FirstAsync(i => i.Id == seed.LineId);

        line.PriceUoM = "";

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Save_guard_lets_an_unrelated_change_to_a_legacy_line_through()
    {
        await using var context = CreateContext();
        var seed = await SeedOrderAsync(context, linePriceUoM: "");
        var line = await context.PurchaseOrderItems.FirstAsync(i => i.Id == seed.LineId);

        line.QuantityInvoiced = 1m;
        await context.SaveChangesAsync();

        Assert.Equal(1m, (await context.PurchaseOrderItems.FirstAsync()).QuantityInvoiced);
    }

    private record OrderSeed(Guid OrderId, Guid LineId, Guid SupplierId, Guid MaterialId, Guid MgId);

    private record QuotationSeed(Guid QuotationId, Guid NewItemId);

    /// <summary>A revised order with one 650 kg line (stored in mg) priced 1.2.</summary>
    private static async Task<OrderSeed> SeedOrderAsync(ApplicationDbContext context, string linePriceUoM)
    {
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Supplier" };
        var material = new Material { Id = Guid.NewGuid(), Name = "Amoxicillin" };
        var mg = new UnitOfMeasure { Id = Guid.NewGuid(), Symbol = "mg" };
        // The revision query includes the source requisition, and the in-memory provider hides
        // an order whose required navigation is missing.
        var source = new SourceRequisition { Id = Guid.NewGuid() };
        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            Code = "EPRC-TEST",
            SupplierId = supplier.Id,
            SourceRequisitionId = source.Id,
            Status = PurchaseOrderStatus.Revised,
        };
        var line = new PurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = order.Id,
            MaterialId = material.Id,
            UoMId = mg.Id,
            Quantity = 650_000_000m,
            Price = 1.2m,
            PriceUoM = linePriceUoM,
        };
        // Seeding a legacy line has to bypass the guard the tests are about.
        WithoutGuard(() =>
        {
            context.AddRange(supplier, material, mg, source, order, line);
            context.SaveChanges();
        });
        context.ChangeTracker.Clear();
        return new OrderSeed(order.Id, line.Id, supplier.Id, material.Id, mg.Id);
    }

    /// <summary>A quotation with one new line in mg and one legacy line priced with no unit.</summary>
    private static async Task<QuotationSeed> SeedQuotationAsync(ApplicationDbContext context)
    {
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Supplier" };
        var material = new Material { Id = Guid.NewGuid(), Name = "Amoxicillin" };
        var mg = new UnitOfMeasure { Id = Guid.NewGuid(), Symbol = "mg" };
        var quotation = new SupplierQuotation { Id = Guid.NewGuid(), SupplierId = supplier.Id };
        var newItem = new SupplierQuotationItem { Id = Guid.NewGuid(), SupplierQuotationId = quotation.Id, MaterialId = material.Id, UoMId = mg.Id };
        var legacy = new SupplierQuotationItem
        {
            Id = Guid.NewGuid(), SupplierQuotationId = quotation.Id, MaterialId = material.Id, UoMId = mg.Id,
            QuotedPrice = 1.5m, PriceUoM = "",
        };
        WithoutGuard(() =>
        {
            context.AddRange(supplier, material, mg, quotation, newItem, legacy);
            context.SaveChanges();
        });
        context.ChangeTracker.Clear();
        return new QuotationSeed(quotation.Id, newItem.Id);
    }

    // Seeding a legacy row needs the guard off. Only this class asserts the guard throws, and
    // xUnit runs a class's tests one at a time, so the brief switch cannot affect another assertion.
    private static void WithoutGuard(Action seed)
    {
        ApplicationDbContext.EnforcePriceUoM = false;
        try { seed(); }
        finally { ApplicationDbContext.EnforcePriceUoM = true; }
    }

    private static IMapper CreateMapper(ApplicationDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton(context);
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static RequisitionRepository CreateRequisitions(ApplicationDbContext context) =>
        new(context, CreateMapper(context), null!, null!, null!, null!, null!, null!, null!,
            new NoOpProductionActivityStepEventPublisher());

    private static ProcurementRepository CreateProcurement(ApplicationDbContext context) =>
        new(context, CreateMapper(context), null!, null!,
            new ApprovalRepository(context, null!, null!, null!, NullLogger<ApprovalRepository>.Instance, null!,
                new NoOpProductionActivityStepEventPublisher()),
            null!, null!);

    private static ApplicationDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new GuardCurrentUser()
        );
}
