using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.Products;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class ProductionOrderPricingTests
{
    private static readonly DateTime Now = new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateProductionOrder_UsesAgreedPrice_WhenOneAgreementIsActive()
    {
        await using var context = CreateContext();
        var ids = Seed(context, listPrice: 10m);
        context.CustomerPricingAgreements.Add(new CustomerPricingAgreement
        {
            Id = Guid.NewGuid(), CustomerId = ids.CustomerId, ProductId = ids.ProductId,
            ProductPackingId = ids.ProductPackingId, AgreedPrice = 42m, CurrencyId = ids.CurrencyId,
            EffectiveFrom = Now.AddDays(-1), EffectiveTo = Now.AddDays(1),
        });
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).CreateProductionOrder(BuildRequest(ids));
        Assert.True(result.IsSuccess);

        var saved = await context.ProductionOrders.Include(o => o.Products)
            .FirstAsync(o => o.Id == result.Value);
        Assert.Equal(42m, saved.Products[0].UnitPrice);
    }

    [Fact]
    public async Task CreateProductionOrder_FallsBackToListPrice_WhenNoAgreementIsActive()
    {
        await using var context = CreateContext();
        var ids = Seed(context, listPrice: 10m);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).CreateProductionOrder(BuildRequest(ids));
        Assert.True(result.IsSuccess);

        var saved = await context.ProductionOrders.Include(o => o.Products)
            .FirstAsync(o => o.Id == result.Value);
        Assert.Equal(10m, saved.Products[0].UnitPrice);
    }

    [Fact]
    public async Task CreateProductionOrder_Fails_WhenTwoAgreementsOverlap()
    {
        await using var context = CreateContext();
        var ids = Seed(context, listPrice: 10m);
        context.CustomerPricingAgreements.AddRange(
            new CustomerPricingAgreement
            {
                Id = Guid.NewGuid(), CustomerId = ids.CustomerId, ProductId = ids.ProductId,
                ProductPackingId = ids.ProductPackingId, AgreedPrice = 42m, CurrencyId = ids.CurrencyId,
                EffectiveFrom = Now.AddDays(-1), EffectiveTo = Now.AddDays(1),
            },
            new CustomerPricingAgreement
            {
                Id = Guid.NewGuid(), CustomerId = ids.CustomerId, ProductId = ids.ProductId,
                ProductPackingId = ids.ProductPackingId, AgreedPrice = 45m, CurrencyId = ids.CurrencyId,
                EffectiveFrom = Now.AddDays(-2), EffectiveTo = Now.AddDays(2),
            });
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).CreateProductionOrder(BuildRequest(ids));

        Assert.True(result.IsFailure);
        Assert.Equal("CustomerPricing.Ambiguous", result.Error.Code);
    }

    private static CreateProductionOrderRequest BuildRequest(TestIds ids) => new()
    {
        Code = "PO-1", CustomerId = ids.CustomerId,
        Products =
        [
            new CreateProductionOrderProduct
            {
                ProductId = ids.ProductId, ProductPackingId = ids.ProductPackingId,
                TotalOrderQuantity = 5, VolumePerPiece = 1,
            }
        ],
    };

    private sealed class TestIds
    {
        public Guid CustomerId { get; init; } = Guid.NewGuid();
        public Guid ProductId { get; init; } = Guid.NewGuid();
        public Guid ProductPackingId { get; init; } = Guid.NewGuid();
        public Guid CurrencyId { get; init; } = Guid.NewGuid();
    }

    private static TestIds Seed(ApplicationDbContext context, decimal listPrice)
    {
        var ids = new TestIds();
        context.AddRange(
            new Customer { Id = ids.CustomerId, Name = "Hospital", CurrencyId = ids.CurrencyId },
            new Product
            {
                Id = ids.ProductId, Name = "Wormee 4 Suspension", Code = "WOR-4",
                Prices = [new ProductPrices { Price = listPrice, Date = Now.AddDays(-30) }],
            },
            new ProductPacking { Id = ids.ProductPackingId, ProductId = ids.ProductId, Name = "150 X 20ml" });
        return ids;
    }

    private static ProductionOrderRepository CreateRepository(ApplicationDbContext context)
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CreateProductionOrderRequest, ProductionOrder>();
            cfg.CreateMap<CreateProductionOrderProduct, ProductionOrderProducts>();
        });
        return new ProductionOrderRepository(context, config.CreateMapper(), null!);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ApplicationDbContext(options, new TestCurrentUser());
    }

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
