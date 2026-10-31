using APP.Repository;
using DOMAIN.Entities.Costing;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Products;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class CostingRepositoryTests
{
    private static readonly DateTime Now = DateTime.UtcNow;

    [Fact]
    public async Task SetListPrice_AppendsAPriceInTheChosenCurrency_AndKeepsTheOldOne()
    {
        await using var context = CreateContext();
        var ids = Seed(context);
        await context.SaveChangesAsync();

        var result = await new CostingRepository(context).SetListPrice(ids.ProductId,
            new SetListPriceRequest { Price = 12.5m, CurrencyId = ids.DollarId }, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        var product = await context.Products.SingleAsync();
        Assert.Equal(2, product.Prices.Count);
        Assert.Equal(12.5m, product.Price);
        Assert.Equal(ids.DollarId, product.PriceCurrencyId);
        Assert.Equal("pack", product.PriceUoM);
    }

    [Fact]
    public async Task SetListPrice_RejectsAnUnknownCurrency()
    {
        await using var context = CreateContext();
        var ids = Seed(context);
        await context.SaveChangesAsync();

        var result = await new CostingRepository(context).SetListPrice(ids.ProductId,
            new SetListPriceRequest { Price = 12.5m, CurrencyId = Guid.NewGuid() }, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Currency.NotFound", result.Error.Code);
        Assert.Single((await context.Products.SingleAsync()).Prices);
    }

    [Fact]
    public async Task GetListPrices_ReadsAPriceWithoutACurrencyAsTheBaseCurrency()
    {
        await using var context = CreateContext();
        var ids = Seed(context);
        await context.SaveChangesAsync();

        var result = await new CostingRepository(context).GetListPrices(1, 10, null, null, null);

        Assert.True(result.IsSuccess);
        var row = Assert.Single(result.Value.Data);
        Assert.Equal(10m, row.Price);
        Assert.Equal(ids.CediId, row.Currency.Id);
    }

    [Fact]
    public async Task GetPricingAgreements_ListsEveryCustomer_AndFiltersByCustomerAndDate()
    {
        await using var context = CreateContext();
        var ids = Seed(context);
        var otherCustomerId = Guid.NewGuid();
        context.Customers.Add(new Customer { Id = otherCustomerId, Name = "Clinic" });
        context.CustomerPricingAgreements.AddRange(
            Agreement(ids, ids.CustomerId, ids.CediId, Now.AddDays(-5), null),
            Agreement(ids, otherCustomerId, ids.DollarId, Now.AddDays(-30), Now.AddDays(-10)));
        await context.SaveChangesAsync();
        var repository = new CostingRepository(context);

        var all = await repository.GetPricingAgreements(1, 10, null, null, null, null);
        var clinic = await repository.GetPricingAgreements(1, 10, null, otherCustomerId, null, null);
        var current = await repository.GetPricingAgreements(1, 10, null, null, null, Now);

        Assert.Equal(2, all.Value.Data.Count());
        var clinicRow = Assert.Single(clinic.Value.Data);
        Assert.Equal("Clinic", clinicRow.CustomerName);
        Assert.Equal("$", clinicRow.Currency.Symbol);
        Assert.Equal(ids.CustomerId, Assert.Single(current.Value.Data).CustomerId);
    }

    private static CustomerPricingAgreement Agreement(TestIds ids, Guid customerId, Guid currencyId, DateTime from, DateTime? to) => new()
    {
        Id = Guid.NewGuid(), CustomerId = customerId, ProductId = ids.ProductId, ProductPackingId = ids.PackingId,
        AgreedPrice = 20m, CurrencyId = currencyId, EffectiveFrom = from, EffectiveTo = to,
    };

    private sealed class TestIds
    {
        public Guid CustomerId { get; } = Guid.NewGuid();
        public Guid ProductId { get; } = Guid.NewGuid();
        public Guid PackingId { get; } = Guid.NewGuid();
        public Guid CediId { get; } = Guid.NewGuid();
        public Guid DollarId { get; } = Guid.NewGuid();
    }

    private static TestIds Seed(ApplicationDbContext context)
    {
        var ids = new TestIds();
        context.AddRange(
            new Currency { Id = ids.CediId, Name = "Cedi", Symbol = "GH₵", IsBaseCurrency = true },
            new Currency { Id = ids.DollarId, Name = "US Dollar", Symbol = "$" },
            new Customer { Id = ids.CustomerId, Name = "Hospital" },
            new Product
            {
                Id = ids.ProductId, Name = "Blopen 100MG", Code = "BLO-100",
                Prices = [new ProductPrices { Price = 10m, PriceUoM = "pack", Date = Now.AddDays(-30) }],
            },
            new ProductPacking { Id = ids.PackingId, ProductId = ids.ProductId, Name = "48 x 20" });
        return ids;
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
