using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Payments;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class PaymentCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class PaymentRepositoryTests
{
    private static readonly DateTime AsOf = new(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, 0, 100)]
    [InlineData(40, 40, 60)]
    [InlineData(100, 100, 0)]
    public async Task Balance_ComputesZeroPartialAndFullPayment(decimal paid, decimal expectedPaid, decimal expectedOutstanding)
    {
        await using var context = CreateContext();
        var ids = new PaymentIds();
        if (paid > 0)
        {
            context.Payments.Add(new Payment
            {
                Id = Guid.NewGuid(), Amount = paid, CurrencyId = ids.UsdId,
                PayableType = PayableType.ShipmentInvoice, PayableId = ids.PayableId,
                Approved = true, RecordedById = Guid.NewGuid(), Reference = $"PAY-{paid}",
            });
            await context.SaveChangesAsync();
        }

        var balances = await PaymentBalanceQuery.GetAsync(
            context,
            PayableType.ShipmentInvoice,
            ids.PayableId,
            [new PaymentBalanceQuery.Total(ids.UsdId, "US Dollar", "$", 100m)]
        );

        var balance = Assert.Single(balances);
        Assert.Equal(expectedPaid, balance.AmountPaid);
        Assert.Equal(expectedOutstanding, balance.OutstandingBalance);
    }

    [Fact]
    public async Task Balance_IgnoresUnapprovedPayment()
    {
        await using var context = CreateContext();
        var ids = new PaymentIds();
        context.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(), Amount = 70m, CurrencyId = ids.UsdId,
            PayableType = PayableType.BillingSheet, PayableId = ids.PayableId,
            Approved = false, RecordedById = Guid.NewGuid(), Reference = "PENDING-1",
        });
        await context.SaveChangesAsync();

        var balance = Assert.Single(await PaymentBalanceQuery.GetAsync(
            context, PayableType.BillingSheet, ids.PayableId,
            [new PaymentBalanceQuery.Total(ids.UsdId, "US Dollar", "$", 100m)]));

        Assert.Equal(0m, balance.AmountPaid);
        Assert.Equal(100m, balance.OutstandingBalance);
    }

    [Theory]
    [InlineData(29, AgingBucket.Days0To30)]
    [InlineData(30, AgingBucket.Days0To30)]
    [InlineData(31, AgingBucket.Days31To60)]
    public void AgingBucket_HandlesBoundaryDates(int overdueDays, AgingBucket expected)
        => Assert.Equal(expected, PaymentRepository.GetAgingBucket(AsOf.AddDays(-overdueDays), AsOf));

    [Fact]
    public async Task ApAging_ConvertsOutstandingUsingLatestEffectiveExchangeRate()
    {
        await using var context = CreateContext();
        var ids = new PaymentIds();
        SeedAging(context, ids);
        await context.SaveChangesAsync();
        var repository = new PaymentRepository(context, CreateMapper());

        var result = await repository.GetApAging(AsOf);

        Assert.True(result.IsSuccess);
        var party = Assert.Single(result.Value.Parties);
        var line = Assert.Single(party.Lines);
        Assert.Equal(80m, line.OriginalOutstanding);
        Assert.Equal(1.2m, line.RateToBase);
        Assert.Equal(96m, line.OutstandingBase);
        Assert.Equal(96m, result.Value.TotalOutstandingBase);
    }

    private static void SeedAging(ApplicationDbContext context, PaymentIds ids)
    {
        context.AddRange(
            new Currency { Id = ids.UsdId, Name = "US Dollar", Symbol = "$", IsBaseCurrency = true },
            new Currency { Id = ids.EurId, Name = "Euro", Symbol = "€" },
            new ExchangeRate { Id = Guid.NewGuid(), CurrencyId = ids.EurId, RateToBase = 1.1m, EffectiveDate = AsOf.AddDays(-10) },
            new ExchangeRate { Id = Guid.NewGuid(), CurrencyId = ids.EurId, RateToBase = 1.2m, EffectiveDate = AsOf.AddDays(-1) },
            new Supplier { Id = ids.SupplierId, Name = "Qualified Supplier" },
            new BillingSheet
            {
                Id = ids.PayableId, Code = "BS-1", InvoiceId = Guid.NewGuid(),
                SupplierId = ids.SupplierId, DueDate = AsOf.AddDays(-30),
                Charges =
                [
                    new BillingSheetCharge
                    {
                        Id = Guid.NewGuid(), CurrencyId = ids.EurId, Amount = 100m,
                    },
                ],
            },
            new Payment
            {
                Id = Guid.NewGuid(), Amount = 20m, CurrencyId = ids.EurId,
                PayableType = PayableType.BillingSheet, PayableId = ids.PayableId,
                Approved = true, RecordedById = Guid.NewGuid(), Reference = "BANK-20",
            }
        );
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, new PaymentCurrentUserService());
    }

    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.CreateMap<Currency, CurrencyDto>(), NullLoggerFactory.Instance);
        return config.CreateMapper();
    }

    private sealed class PaymentIds
    {
        public Guid UsdId { get; } = Guid.NewGuid();
        public Guid EurId { get; } = Guid.NewGuid();
        public Guid SupplierId { get; } = Guid.NewGuid();
        public Guid PayableId { get; } = Guid.NewGuid();
    }
}
