using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Products;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class CustomerQuotationPricingTests
{
    private static readonly DateTime AsOf = new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ResolveQuotationUnitPrice_UsesAgreedPrice_WhenOneAgreementIsActive()
    {
        await using var context = CreateContext();
        var ids = Seed(context, listPrice: 10m);
        context.CustomerPricingAgreements.Add(new CustomerPricingAgreement
        {
            Id = Guid.NewGuid(), CustomerId = ids.CustomerId, ProductId = ids.ProductId,
            ProductPackingId = ids.ProductPackingId, AgreedPrice = 42m, CurrencyId = ids.CurrencyId,
            EffectiveFrom = AsOf.AddDays(-1), EffectiveTo = AsOf.AddDays(1),
        });
        await context.SaveChangesAsync();

        var result = await CreateRepository(context)
            .ResolveQuotationUnitPrice(ids.CustomerId, ids.ProductId, ids.ProductPackingId, AsOf);

        Assert.True(result.IsSuccess);
        Assert.Equal(42m, result.Value.UnitPrice);
        Assert.True(result.Value.FromAgreement);
    }

    [Fact]
    public async Task ResolveQuotationUnitPrice_UsesAgreement_WhenCustomerHasNoPreferredCurrency()
    {
        await using var context = CreateContext();
        var ids = Seed(context, listPrice: 10m);
        context.Customers.Local.Single().CurrencyId = null;
        context.CustomerPricingAgreements.Add(new CustomerPricingAgreement
        {
            Id = Guid.NewGuid(), CustomerId = ids.CustomerId, ProductId = ids.ProductId,
            ProductPackingId = ids.ProductPackingId, AgreedPrice = 42m, CurrencyId = ids.CurrencyId,
            EffectiveFrom = AsOf.AddDays(-1), EffectiveTo = AsOf.AddDays(1),
        });
        await context.SaveChangesAsync();

        var result = await CreateRepository(context)
            .ResolveQuotationUnitPrice(ids.CustomerId, ids.ProductId, ids.ProductPackingId, AsOf);

        Assert.True(result.IsSuccess);
        Assert.Equal(42m, result.Value.UnitPrice);
        Assert.True(result.Value.FromAgreement);
    }

    [Fact]
    public async Task CreateQuotation_InfersCurrencyFromActivePricingAgreement()
    {
        await using var context = CreateContext();
        var ids = Seed(context, listPrice: 10m);
        context.Customers.Local.Single().CurrencyId = null;
        context.CustomerPricingAgreements.Add(new CustomerPricingAgreement
        {
            Id = Guid.NewGuid(), CustomerId = ids.CustomerId, ProductId = ids.ProductId,
            ProductPackingId = ids.ProductPackingId, AgreedPrice = 42m, CurrencyId = ids.CurrencyId,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1), EffectiveTo = DateTime.UtcNow.AddDays(1),
        });
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).CreateQuotation(
            ids.CustomerId,
            new CreateCustomerQuotationRequest
            {
                Code = "Q-AGREEMENT-CURRENCY",
                ValidUntil = DateTime.UtcNow.AddDays(7),
                Items =
                [
                    new CreateCustomerQuotationItemRequest
                    {
                        ProductId = ids.ProductId,
                        ProductPackingId = ids.ProductPackingId,
                        Quantity = 165,
                    },
                ],
            },
            Guid.NewGuid());

        Assert.True(result.IsSuccess);
        var quotation = await context.CustomerQuotations.Include(item => item.Items)
            .SingleAsync(item => item.Id == result.Value);
        Assert.Equal(ids.CurrencyId, quotation.CurrencyId);
        Assert.Equal(42m, quotation.Items.Single().UnitPrice);
    }

    [Fact]
    public async Task ResolveQuotationUnitPrice_FallsBackToListPrice_WhenNoAgreementIsActive()
    {
        await using var context = CreateContext();
        var ids = Seed(context, listPrice: 10m);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context)
            .ResolveQuotationUnitPrice(ids.CustomerId, ids.ProductId, ids.ProductPackingId, AsOf);

        Assert.True(result.IsSuccess);
        Assert.Equal(10m, result.Value.UnitPrice);
        Assert.False(result.Value.FromAgreement);
    }

    [Fact]
    public async Task ResolveQuotationUnitPrice_IsAmbiguous_WhenTwoAgreementsOverlap()
    {
        await using var context = CreateContext();
        var ids = Seed(context, listPrice: 10m);
        context.CustomerPricingAgreements.AddRange(
            new CustomerPricingAgreement
            {
                Id = Guid.NewGuid(), CustomerId = ids.CustomerId, ProductId = ids.ProductId,
                ProductPackingId = ids.ProductPackingId, AgreedPrice = 42m, CurrencyId = ids.CurrencyId,
                EffectiveFrom = AsOf.AddDays(-1), EffectiveTo = AsOf.AddDays(1),
            },
            new CustomerPricingAgreement
            {
                Id = Guid.NewGuid(), CustomerId = ids.CustomerId, ProductId = ids.ProductId,
                ProductPackingId = ids.ProductPackingId, AgreedPrice = 45m, CurrencyId = ids.CurrencyId,
                EffectiveFrom = AsOf.AddDays(-2), EffectiveTo = AsOf.AddDays(2),
            });
        await context.SaveChangesAsync();

        var result = await CreateRepository(context)
            .ResolveQuotationUnitPrice(ids.CustomerId, ids.ProductId, ids.ProductPackingId, AsOf);

        Assert.True(result.IsFailure);
        Assert.Equal("CustomerPricing.Ambiguous", result.Error.Code);
    }

    [Fact]
    public async Task ResolveQuotationUnitPrice_Conflicts_WhenAgreementCurrencyDiffersFromCustomer()
    {
        await using var context = CreateContext();
        var ids = Seed(context, listPrice: 10m);
        var otherCurrencyId = Guid.NewGuid();
        context.Currencies.Add(new Currency { Id = otherCurrencyId, Name = "Dollar", Symbol = "$" });
        context.CustomerPricingAgreements.Add(new CustomerPricingAgreement
        {
            Id = Guid.NewGuid(), CustomerId = ids.CustomerId, ProductId = ids.ProductId,
            ProductPackingId = ids.ProductPackingId, AgreedPrice = 42m, CurrencyId = otherCurrencyId,
            EffectiveFrom = AsOf.AddDays(-1), EffectiveTo = AsOf.AddDays(1),
        });
        await context.SaveChangesAsync();

        var result = await CreateRepository(context)
            .ResolveQuotationUnitPrice(ids.CustomerId, ids.ProductId, ids.ProductPackingId, AsOf);

        Assert.True(result.IsFailure);
        Assert.Equal("CustomerPricing.Currency", result.Error.Code);
    }

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
            new Currency { Id = ids.CurrencyId, Name = "Cedi", Symbol = "GHS", IsBaseCurrency = true },
            new Customer { Id = ids.CustomerId, Name = "Hospital", CurrencyId = ids.CurrencyId },
            new Product
            {
                Id = ids.ProductId, Name = "Wormee 4 Suspension", Code = "WOR-4",
                Prices = [new ProductPrices { Price = listPrice, Date = AsOf.AddDays(-30) }],
            },
            new ProductPacking { Id = ids.ProductPackingId, ProductId = ids.ProductId, Name = "150 X 20ml" });
        return ids;
    }

    private static CustomerRepository CreateRepository(ApplicationDbContext context)
    {
        var config = new MapperConfiguration(
            cfg => cfg.CreateMap<CreateCustomerRequest, Customer>());
        var approvalRepository = new ApprovalRepository(
            context, null!, null!, null!,
            NullLogger<ApprovalRepository>.Instance, null!,
            new NoOpProductionActivityStepEventPublisher());
        return new CustomerRepository(context, config.CreateMapper(), approvalRepository);
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
