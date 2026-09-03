using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Invoices;
using DOMAIN.Entities.Payments;
using DOMAIN.Entities.Products;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using System.Text.Json;
using Xunit;

namespace APP.Tests.Repository;

public class CustomerRepositoryTests
{
    private static readonly DateTime AsOf = new(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, 300, 700)]
    [InlineData(100, 200, 800)]
    [InlineData(300, 0, 1000)]
    public async Task CreditStatus_ComputesZeroAndPartialPayment(
        decimal paid, decimal expectedOutstanding, decimal expectedAvailable)
    {
        await using var context = CreateContext();
        var ids = SeedCredit(context, paid);
        await context.SaveChangesAsync();
        var repository = CreateRepository(context);

        var result = await repository.GetAvailableCredit(ids.CustomerId);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedOutstanding, result.Value.OutstandingInPreferredCurrency);
        Assert.Equal(expectedAvailable, result.Value.AvailableCredit);
    }

    [Fact]
    public async Task SendQuotation_AutoApproves_WhenNoWorkflowConfigured()
    {
        // Regression: SendQuotation used to hard-block with "CustomerQuotation.ApprovalMissing"
        // whenever no approval workflow existed - meaning nobody could ever send a
        // quotation until an admin configured one. It must auto-approve instead.
        await using var context = CreateContext();
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Hospital" };
        var quotation = new CustomerQuotation
        {
            Id = Guid.NewGuid(), CustomerId = customer.Id, Code = "Q-AUTO",
            CurrencyId = Guid.NewGuid(), Status = CustomerQuotationStatus.Draft,
            ValidUntil = DateTime.UtcNow.AddDays(10),
        };
        context.AddRange(customer, quotation);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).SendQuotation(quotation.Id, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(CustomerQuotationStatus.Accepted, quotation.Status);
        Assert.True(quotation.Approved);
        Assert.Empty(quotation.Approvals);
        Assert.Single(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task SendQuotation_CreatesConfiguredPendingApproval_WhenWorkflowExists()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Hospital" };
        var quotation = new CustomerQuotation
        {
            Id = Guid.NewGuid(), CustomerId = customer.Id, Code = "Q-CONFIGURED",
            CurrencyId = Guid.NewGuid(), Status = CustomerQuotationStatus.Draft,
            ValidUntil = DateTime.UtcNow.AddDays(10),
        };
        var approval = new Approval
        {
            Id = Guid.NewGuid(), ItemType = nameof(CustomerQuotation), ApprovalStages = [],
        };
        approval.ApprovalStages.Add(new ApprovalStage
        {
            Id = Guid.NewGuid(), ApprovalId = approval.Id, Approval = approval,
            Order = 1, Required = true, UserId = approverId,
        });
        context.AddRange(customer, quotation, approval);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).SendQuotation(quotation.Id, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(CustomerQuotationStatus.Sent, quotation.Status);
        Assert.False(quotation.Approved);
        var stage = Assert.Single(quotation.Approvals);
        Assert.Equal(approverId, stage.UserId);
        Assert.Empty(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task Conversion_CarriesNegotiatedPriceAndDiscount()
    {
        await using var context = CreateContext();
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Hospital" };
        var product = new Product { Id = Guid.NewGuid(), Name = "Tablets", BaseQuantity = 5m };
        var uom = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Carton" };
        var quotation = new CustomerQuotation
        {
            Id = Guid.NewGuid(), CustomerId = customer.Id, Code = "Q-100",
            CurrencyId = Guid.NewGuid(), Status = CustomerQuotationStatus.Accepted,
            Approved = true, ValidUntil = DateTime.UtcNow.AddDays(10),
            Items =
            [
                new CustomerQuotationItem
                {
                    Id = Guid.NewGuid(), ProductId = product.Id, Product = product,
                    UoMId = uom.Id, Quantity = 2, UnitPrice = 100m, DiscountPercent = 10m,
                },
            ],
        };
        context.AddRange(customer, product, uom, quotation);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).ConvertQuotationToProductionOrder(quotation.Id, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        var order = await context.ProductionOrders.Include(item => item.Products)
            .ThenInclude(item => item.Product).SingleAsync(item => item.Id == result.Value);
        var line = Assert.Single(order.Products);
        Assert.Equal(100m, line.UnitPrice);
        Assert.Equal(10m, line.DiscountPercent);
        Assert.Equal(180m, line.TotalValue);
        Assert.Equal(quotation.Id, order.SourceCustomerQuotationId);
        Assert.Equal(CustomerQuotationStatus.ConvertedToOrder, quotation.Status);
    }

    [Fact]
    public async Task PricingAgreement_IsActiveOnInclusiveEndBoundary()
    {
        await using var context = CreateContext();
        var ids = new TestIds();
        context.AddRange(
            new Customer { Id = ids.CustomerId, Name = "Distributor" },
            new Product { Id = ids.ProductId, Name = "Capsules" },
            new UnitOfMeasure { Id = ids.UomId, Name = "Case" },
            new Currency { Id = ids.CurrencyId, Name = "Cedi" },
            new CustomerPricingAgreement
            {
                Id = Guid.NewGuid(), CustomerId = ids.CustomerId, ProductId = ids.ProductId,
                UoMId = ids.UomId, CurrencyId = ids.CurrencyId, AgreedPrice = 25m,
                EffectiveFrom = AsOf.AddDays(-30), EffectiveTo = AsOf,
            });
        await context.SaveChangesAsync();

        var result = await CreateRepository(context)
            .GetActivePricingAgreement(ids.CustomerId, ids.ProductId, ids.UomId, AsOf);

        Assert.True(result.IsSuccess);
        Assert.Equal(25m, result.Value.AgreedPrice);
    }

    [Fact]
    public async Task ActiveQuotations_ExcludeExpiredDrafts()
    {
        await using var context = CreateContext();
        var ids = new TestIds();
        context.AddRange(
            new Currency { Id = ids.CurrencyId, Name = "Cedi", IsBaseCurrency = true },
            new Customer { Id = ids.CustomerId, Name = "Pharmacy", CurrencyId = ids.CurrencyId },
            Quote(ids, "EXPIRED", AsOf.AddTicks(-1)),
            Quote(ids, "ACTIVE", AsOf));
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetActiveQuotations(ids.CustomerId, 1, 10, AsOf);

        Assert.True(result.IsSuccess);
        Assert.Equal("ACTIVE", Assert.Single(result.Value.Data).Code);
    }

    [Fact]
    public async Task LegacyUpdate_PreservesOptionalCrmFields()
    {
        await using var context = CreateContext();
        var currencyId = Guid.NewGuid();
        var customer = new Customer
        {
            Id = Guid.NewGuid(), Name = "Original Hospital", Email = "old@example.com",
            Phone = "+233200000000", Address = "Old address", CreditLimit = 500m,
            CurrencyId = currencyId, BillingAddress = "Accounts office",
        };
        context.AddRange(new Currency { Id = currencyId, Name = "Cedi" }, customer);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).UpdateCustomer(customer.Id, new CreateCustomerRequest
        {
            Name = "Updated Hospital", Email = "new@example.com",
            Phone = "+233200000001", Address = "New address",
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(500m, customer.CreditLimit);
        Assert.Equal(currencyId, customer.CurrencyId);
        Assert.Equal("Accounts office", customer.BillingAddress);
    }

    [Fact]
    public async Task Update_ExplicitNullClearsCreditFieldsButPreservesOmittedFields()
    {
        await using var context = CreateContext();
        var currencyId = Guid.NewGuid();
        var customer = new Customer
        {
            Id = Guid.NewGuid(), Name = "Original Hospital", Email = "old@example.com",
            Phone = "+233200000000", Address = "Old address", CreditLimit = 500m,
            CurrencyId = currencyId, BillingAddress = "Accounts office",
        };
        context.AddRange(new Currency { Id = currencyId, Name = "Cedi" }, customer);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).UpdateCustomer(customer.Id, new CreateCustomerRequest
        {
            Name = "Updated Hospital", Email = "new@example.com", Phone = "+233200000001",
            Address = "New address", CreditLimit = null, CurrencyId = null,
        });

        Assert.True(result.IsSuccess);
        Assert.Null(customer.CreditLimit);
        Assert.Null(customer.CurrencyId);
        Assert.Equal("Accounts office", customer.BillingAddress);
    }

    [Fact]
    public void CustomerRequest_TracksOmittedAndExplicitNullCrmFields()
    {
        var omitted = JsonSerializer.Deserialize<CreateCustomerRequest>("{}");
        var explicitNull = JsonSerializer.Deserialize<CreateCustomerRequest>(
            """{"CreditLimit":null,"CurrencyId":null,"BillingAddress":null}""");

        Assert.False(omitted!.CreditLimitProvided);
        Assert.False(omitted.CurrencyIdProvided);
        Assert.False(omitted.BillingAddressProvided);
        Assert.True(explicitNull!.CreditLimitProvided);
        Assert.True(explicitNull.CurrencyIdProvided);
        Assert.True(explicitNull.BillingAddressProvided);
    }

    private static CustomerQuotation Quote(TestIds ids, string code, DateTime validUntil) => new()
    {
        Id = Guid.NewGuid(), CustomerId = ids.CustomerId, CurrencyId = ids.CurrencyId,
        Code = code, Status = CustomerQuotationStatus.Draft, ValidUntil = validUntil,
    };

    private static TestIds SeedCredit(ApplicationDbContext context, decimal paid)
    {
        var ids = new TestIds { InvoiceId = Guid.NewGuid() };
        context.AddRange(
            new Currency { Id = ids.CurrencyId, Name = "Cedi", Symbol = "GHS", IsBaseCurrency = true },
            new Customer { Id = ids.CustomerId, Name = "Hospital", CreditLimit = 1000m, CurrencyId = ids.CurrencyId },
            new Invoice
            {
                Id = ids.InvoiceId, CustomerId = ids.CustomerId, Status = InvoiceStatus.Approved,
                ProformaInvoiceId = Guid.NewGuid(),
                Amounts = [new InvoiceAmount { Id = Guid.NewGuid(), CurrencyId = ids.CurrencyId, Amount = 300m }],
            });
        if (paid > 0) context.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(), Amount = paid, CurrencyId = ids.CurrencyId, Approved = true,
            PayableType = PayableType.CustomerInvoice, PayableId = ids.InvoiceId,
            RecordedById = Guid.NewGuid(), Reference = "PAY-CRM",
        });
        return ids;
    }

    private static CustomerRepository CreateRepository(ApplicationDbContext context)
    {
        var config = new MapperConfiguration(
            cfg => cfg.CreateMap<CreateCustomerRequest, Customer>(), NullLoggerFactory.Instance);
        var approvalRepository = new ApprovalRepository(
            context,
            null!,
            null!,
            null!,
            NullLogger<ApprovalRepository>.Instance,
            null!,
            new NoOpProductionActivityStepEventPublisher()
        );
        return new CustomerRepository(context, config.CreateMapper(), approvalRepository);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ApplicationDbContext(options, new TestCurrentUser());
    }

    private sealed class TestIds
    {
        public Guid CustomerId { get; init; } = Guid.NewGuid();
        public Guid ProductId { get; init; } = Guid.NewGuid();
        public Guid UomId { get; init; } = Guid.NewGuid();
        public Guid CurrencyId { get; init; } = Guid.NewGuid();
        public Guid InvoiceId { get; init; }
    }

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
