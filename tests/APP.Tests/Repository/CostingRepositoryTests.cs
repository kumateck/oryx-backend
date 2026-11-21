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
    public async Task GetListPrices_ReturnsOnlyTheLatestPrice_AndKeepsUnpricedProducts()
    {
        await using var context = CreateContext();
        var ids = Seed(context);
        var product = context.Products.Local.Single();
        product.Prices.Add(new ProductPrices { Price = 12.5m, CurrencyId = ids.DollarId, PriceUoM = "bottle", Date = Now });
        context.Products.AddRange(new Product { Id = Guid.NewGuid(), Name = "Unpriced" },
            new Product { Id = Guid.NewGuid(), Name = "Deleted", DeletedAt = Now });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await new CostingRepository(context).GetListPrices(1, 30, "", null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalRecordCount);
        var rows = result.Value.Data.ToList();
        var priced = Assert.Single(rows, row => row.ProductId == ids.ProductId);
        Assert.Equal(12.5m, priced.Price);
        Assert.Equal("bottle", priced.PriceUoM);
        Assert.Equal(Now, priced.PricedAt);
        Assert.Equal(ids.DollarId, priced.Currency.Id);
        var unpriced = Assert.Single(rows, row => row.ProductName == "Unpriced");
        Assert.Equal(0, unpriced.Price);
        Assert.Null(unpriced.PricedAt);
        Assert.Null(unpriced.Currency);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetListPrices_FiltersByLatestCurrency_AndPaginatesWithoutDuplicatingHistory()
    {
        await using var context = CreateContext();
        var ids = Seed(context);
        context.Products.Local.Single().Prices.Add(new ProductPrices { Price = 20m, CurrencyId = ids.DollarId, Date = Now });
        for (var index = 0; index < 32; index++)
            context.Products.Add(new Product { Id = Guid.NewGuid(), Name = $"Product {index:D2}", Prices = [
                new ProductPrices { Price = 1m, CurrencyId = ids.DollarId, Date = Now.AddDays(-1) },
                new ProductPrices { Price = 2m, CurrencyId = ids.CediId, Date = Now },
            ] });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new CostingRepository(context);

        var dollars = await repository.GetListPrices(1, 30, "", null, ids.DollarId);
        Assert.Equal(ids.ProductId, Assert.Single(dollars.Value.Data).ProductId);
        var first = await repository.GetListPrices(1, 30, "", null, ids.CediId);
        var second = await repository.GetListPrices(2, 30, "", null, ids.CediId);
        Assert.Equal(32, first.Value.TotalRecordCount);
        Assert.Equal(2, first.Value.PageCount);
        Assert.Equal(30, first.Value.Data.Count());
        Assert.Equal(2, second.Value.Data.Count());
        Assert.Empty(first.Value.Data.Select(row => row.ProductId).Intersect(second.Value.Data.Select(row => row.ProductId)));
        Assert.All(first.Value.Data.Concat(second.Value.Data), row => Assert.Equal(2m, row.Price));
        Assert.Empty(context.ChangeTracker.Entries());
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
