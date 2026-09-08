using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Payments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class PaymentRegisterTests
{
    [Fact]
    public async Task Register_IncludesAllStatusesAndPaginatesFilteredReceipts()
    {
        await using var context = CreateContext();
        var currency = new Currency { Id = Guid.NewGuid(), Name = "Cedi", Symbol = "GH₵", IsBaseCurrency = true };
        context.Currencies.Add(currency);
        for (var index = 0; index < 6; index++)
            context.Payments.Add(new Payment
            {
                Id = Guid.NewGuid(), CurrencyId = currency.Id, Amount = 10,
                Reference = $"REF-{index}", PaymentDate = DateTime.UtcNow.Date.AddDays(index),
                PayableType = index < 3 ? PayableType.CustomerInvoice : PayableType.BillingSheet,
                Status = (PaymentStatus)(index % 3), RecordedById = Guid.NewGuid(),
            });
        await context.SaveChangesAsync();
        var repository = CreateRepository(context);
        var all = await repository.GetPayments(new PaymentListRequest());
        Assert.Equal(6, all.Value.TotalRecordCount);
        var receipts = await repository.GetPayments(new PaymentListRequest { IsReceipt = true, PageSize = 2, Page = 2 });
        Assert.Equal(3, receipts.Value.TotalRecordCount);
        Assert.Equal(2, receipts.Value.PageCount);
        Assert.Equal("REF-0", Assert.Single(receipts.Value.Data).Reference);
        var pending = await repository.GetPayments(new PaymentListRequest { Status = PaymentStatus.Pending, SearchQuery = "REF-3" });
        Assert.Equal("REF-3", Assert.Single(pending.Value.Data).Reference);
        Assert.False((await repository.GetPayments(new PaymentListRequest { Page = 0 })).IsSuccess);
    }

    [Fact]
    public async Task Rates_ShowLatestEffectiveRateMissingRateAndBaseIdentity()
    {
        await using var context = CreateContext();
        var asOf = new DateTime(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc);
        var usd = new Currency { Id = Guid.NewGuid(), Name = "USD" };
        var missing = new Currency { Id = Guid.NewGuid(), Name = "EUR" };
        context.AddRange(usd, missing, new Currency { Id = Guid.NewGuid(), Name = "GHS", IsBaseCurrency = true });
        context.ExchangeRates.AddRange(
            new ExchangeRate { Id = Guid.NewGuid(), CurrencyId = usd.Id, RateToBase = 10, EffectiveDate = asOf.AddDays(-2) },
            new ExchangeRate { Id = Guid.NewGuid(), CurrencyId = usd.Id, RateToBase = 11, EffectiveDate = asOf.AddDays(-1) },
            new ExchangeRate { Id = Guid.NewGuid(), CurrencyId = usd.Id, RateToBase = 12, EffectiveDate = asOf.AddDays(1) });
        await context.SaveChangesAsync();
        var result = (await CreateRepository(context).GetCurrencyConfiguration(asOf)).Value;
        Assert.Equal("GHS", result.BaseCurrency!.Name);
        Assert.Equal(11m, result.Rates.Single(rate => rate.Currency.Id == usd.Id).RateToBase);
        Assert.Equal(asOf.AddDays(-1), result.Rates.Single(rate => rate.Currency.Id == usd.Id).EffectiveDate);
        Assert.Null(result.Rates.Single(rate => rate.Currency.Id == missing.Id).RateToBase);
        Assert.Equal(1m, result.Rates.Single(rate => rate.Currency.IsBaseCurrency).RateToBase);
    }

    [Fact]
    public async Task Rates_DoNotClaimConversionWithoutBaseCurrency()
    {
        await using var context = CreateContext();
        context.Currencies.Add(new Currency { Id = Guid.NewGuid(), Name = "USD" });
        await context.SaveChangesAsync();
        var result = (await CreateRepository(context).GetCurrencyConfiguration()).Value;
        Assert.Null(result.BaseCurrency);
        Assert.Null(Assert.Single(result.Rates).RateToBase);
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new RegisterCurrentUser());

    private static PaymentRepository CreateRepository(ApplicationDbContext context) => new(context,
        new MapperConfiguration(cfg => cfg.CreateMap<Currency, CurrencyDto>(), NullLoggerFactory.Instance).CreateMapper());

    private sealed class RegisterCurrentUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
