using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Invoices;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.ProformaInvoices;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>An invoice's saved amount is packs x the customer's agreed price per pack, else packs x the list price.</summary>
public class InvoicePricingTests
{
    private static readonly DateTime Now = DateTime.UtcNow;

    [Fact]
    public async Task CreateInvoice_UsesAgreedPricePerPack_InTheAgreementCurrency()
    {
        await using var context = CreateContext();
        var ids = Seed(context);
        context.CustomerPricingAgreements.Add(new CustomerPricingAgreement
        {
            Id = Guid.NewGuid(), CustomerId = ids.CustomerId, ProductId = ids.ProductId,
            ProductPackingId = ids.ProductPackingId, AgreedPrice = 42m, CurrencyId = ids.AgreementCurrencyId,
            EffectiveFrom = Now.AddDays(-1),
        });
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).CreateInvoice(new CreateInvoice { ProformaInvoiceId = ids.ProformaId, CustomerId = ids.CustomerId });

        Assert.True(result.IsSuccess);
        var amount = Assert.Single(await context.InvoiceAmounts.Where(item => item.InvoiceId == result.Value).ToListAsync());
        Assert.Equal(ids.AgreementCurrencyId, amount.CurrencyId);
        Assert.Equal(5 * 42m, amount.Amount);
    }

    [Fact]
    public async Task CreateInvoice_UsesListPrice_InTheBaseCurrency_WhenNoAgreementIsActive()
    {
        await using var context = CreateContext();
        var ids = Seed(context);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).CreateInvoice(new CreateInvoice { ProformaInvoiceId = ids.ProformaId, CustomerId = ids.CustomerId });

        Assert.True(result.IsSuccess);
        var amount = Assert.Single(await context.InvoiceAmounts.Where(item => item.InvoiceId == result.Value).ToListAsync());
        Assert.Equal(ids.BaseCurrencyId, amount.CurrencyId);
        Assert.Equal(5 * 10m, amount.Amount);
    }

    private sealed class TestIds
    {
        public Guid CustomerId { get; } = Guid.NewGuid();
        public Guid ProductId { get; } = Guid.NewGuid();
        public Guid ProductPackingId { get; } = Guid.NewGuid();
        public Guid BaseCurrencyId { get; } = Guid.NewGuid();
        public Guid AgreementCurrencyId { get; } = Guid.NewGuid();
        public Guid ProformaId { get; } = Guid.NewGuid();
    }

    private static TestIds Seed(ApplicationDbContext context)
    {
        var ids = new TestIds();
        var orderId = Guid.NewGuid();
        var allocationId = Guid.NewGuid();
        context.AddRange(
            new Currency { Id = ids.BaseCurrencyId, Name = "Cedi", Symbol = "GH₵", IsBaseCurrency = true },
            new Currency { Id = ids.AgreementCurrencyId, Name = "US Dollar", Symbol = "$" },
            new Customer { Id = ids.CustomerId, Name = "Tobinco", CurrencyId = ids.BaseCurrencyId },
            new Product
            {
                Id = ids.ProductId, Name = "Blopen 100MG", Code = "BLO-100",
                Prices = [new ProductPrices { Price = 10m, Date = Now.AddDays(-30) }],
            },
            new ProductPacking { Id = ids.ProductPackingId, ProductId = ids.ProductId, Name = "48 x 20 x 1 x 10" },
            new ProductionOrder { Id = orderId, Code = "PO-1", CustomerId = ids.CustomerId },
            new AllocateProductionOrder
            {
                Id = allocationId, ProductionOrderId = orderId, Approved = true,
                Products = [new AllocateProductionOrderProduct { ProductId = ids.ProductId, ProductPackingId = ids.ProductPackingId }],
            },
            new ProformaInvoice
            {
                Id = ids.ProformaId, Code = "PI-1", AllocateProductionOrderId = allocationId, Approved = true,
                Products = [new ProformaInvoiceProduct { Id = Guid.NewGuid(), ProductId = ids.ProductId, Quantity = 5 }],
            });
        return ids;
    }

    private static ProductionOrderRepository CreateRepository(ApplicationDbContext context)
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CreateInvoice, Invoice>();
            cfg.CreateMap<CreateInvoiceAmount, InvoiceAmount>();
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
