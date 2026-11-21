using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Roles;
using Microsoft.AspNetCore.Identity;
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
    public async Task CreateInvoice_PreservesTheProformasAgreedPackPriceAndCurrency()
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

        await FreezeProforma(context, ids, 42m, ids.AgreementCurrencyId);
        var result = await CreateRepository(context).CreateInvoice(new CreateInvoice { Code = "INV-TEST", ProformaInvoiceId = ids.ProformaId, CustomerId = ids.CustomerId });

        Assert.True(result.IsSuccess);
        var amount = Assert.Single(await context.InvoiceAmounts.Where(item => item.InvoiceId == result.Value).ToListAsync());
        Assert.Equal(ids.AgreementCurrencyId, amount.CurrencyId);
        Assert.Equal(5 * 42m, amount.Amount);
    }

    [Fact]
    public async Task CreateInvoice_PreservesTheProformasBaseCurrencyPackPrice()
    {
        await using var context = CreateContext();
        var ids = Seed(context);
        await context.SaveChangesAsync();

        await FreezeProforma(context, ids, 10m, ids.BaseCurrencyId);
        var result = await CreateRepository(context).CreateInvoice(new CreateInvoice { Code = "INV-TEST", ProformaInvoiceId = ids.ProformaId, CustomerId = ids.CustomerId });

        Assert.True(result.IsSuccess);
        var amount = Assert.Single(await context.InvoiceAmounts.Where(item => item.InvoiceId == result.Value).ToListAsync());
        Assert.Equal(ids.BaseCurrencyId, amount.CurrencyId);
        Assert.Equal(5 * 10m, amount.Amount);
    }

    [Fact]
    public async Task CreateInvoice_PreservesTheProformasForeignCurrencyPackPrice()
    {
        await using var context = CreateContext();
        var ids = Seed(context);
        context.Products.Local.Single().Prices.Single().CurrencyId = ids.AgreementCurrencyId;
        await context.SaveChangesAsync();

        await FreezeProforma(context, ids, 10m, ids.AgreementCurrencyId);
        var result = await CreateRepository(context).CreateInvoice(new CreateInvoice { Code = "INV-TEST", ProformaInvoiceId = ids.ProformaId, CustomerId = ids.CustomerId });

        Assert.True(result.IsSuccess);
        var amount = Assert.Single(await context.InvoiceAmounts.Where(item => item.InvoiceId == result.Value).ToListAsync());
        Assert.Equal(ids.AgreementCurrencyId, amount.CurrencyId);
        Assert.Equal(5 * 10m, amount.Amount);
    }

    [Fact]
    public async Task Proforma_and_invoice_keep_the_exact_saved_pack_amount_and_currency_when_later_prices_change()
    {
        await using var context=CreateContext();var ids=Seed(context);
        var allocation=context.AllocateProductionOrders.Local.Single();var order=context.ProductionOrders.Local.Single();
        var second=Guid.NewGuid();var revision=Guid.NewGuid();
        context.ProductPackings.Add(new ProductPacking {Id=second,ProductId=ids.ProductId,Name="24 bottles"});
        allocation.Products.Add(new() {ProductId=ids.ProductId,ProductPackingId=second,FulfilledQuantities=[new(){FinishedGoodsTransferNoteId=Guid.NewGuid(),Quantity=2}]});
        order.Products=[new() {ProductId=ids.ProductId,ProductPackingId=ids.ProductPackingId,UnitPrice=12m,PriceCurrencyId=ids.BaseCurrencyId,PriceListRevisionId=revision},
            new() {ProductId=ids.ProductId,ProductPackingId=second,UnitPrice=240m,PriceCurrencyId=ids.AgreementCurrencyId,PriceListRevisionId=revision}];
        await context.SaveChangesAsync();
        context.ProformaInvoiceProducts.RemoveRange(await context.ProformaInvoiceProducts.Where(p=>p.ProformaInvoiceId==ids.ProformaId).ToListAsync());
        context.ProformaInvoices.Remove(await context.ProformaInvoices.SingleAsync(p=>p.Id==ids.ProformaId));
        await context.SaveChangesAsync();
        var repository=CreateRepository(context);
        var ambiguous=await repository.CreateProformaInvoice(new() {AllocateProductionOrderId=allocation.Id,Code="PI-ambiguous",Products=[new() {ProductId=ids.ProductId,Quantity=1}]});
        Assert.Equal("Invoice.Packing",ambiguous.Errors[0].Code);
        var created=await repository.CreateProformaInvoice(new() {AllocateProductionOrderId=allocation.Id,Code="PI-new",Products=[
            new() {ProductId=ids.ProductId,ProductPackingId=ids.ProductPackingId,Quantity=5},new() {ProductId=ids.ProductId,ProductPackingId=second,Quantity=2}]});
        Assert.True(created.IsSuccess);
        var proforma=await context.ProformaInvoices.Include(item=>item.Products).SingleAsync(item=>item.Id==created.Value);
        Assert.Equal(240m,proforma.Products.Single(item=>item.ProductPackingId==second).UnitPrice);
        Assert.All(proforma.Products,item=>Assert.Equal(revision,item.PriceListRevisionId));
        proforma.Approved=true;proforma.CustomerPoVerifiedAt=Now;proforma.CustomerPoAttachmentId=Guid.NewGuid();(await context.Products.Include(p=>p.Prices).SingleAsync(p=>p.Id==ids.ProductId)).Prices.Single().Price=999m;await context.SaveChangesAsync();
        var invoice=await repository.CreateInvoice(new CreateInvoice {Code="INV-TEST",ProformaInvoiceId=proforma.Id,CustomerId=ids.CustomerId});
        Assert.True(invoice.IsSuccess);
        var amounts=await context.InvoiceAmounts.Where(item=>item.InvoiceId==invoice.Value).ToListAsync();
        Assert.Equal(60m,amounts.Single(item=>item.CurrencyId==ids.BaseCurrencyId).Amount);
        Assert.Equal(480m,amounts.Single(item=>item.CurrencyId==ids.AgreementCurrencyId).Amount);
    }

    [Fact]
    public async Task Effective_price_list_with_an_unpriced_style_cannot_create_an_empty_invoice()
    {
        await using var context=CreateContext();var ids=Seed(context);
        context.CostingPriceLists.Add(new DOMAIN.Entities.Costing.CostingPriceList {Id=Guid.NewGuid(),Division=0,RevisionNumber=0,
            EffectiveFrom=DateTime.UtcNow.AddDays(-1),ApprovedAt=DateTime.UtcNow.AddDays(-1),Status=DOMAIN.Entities.Costing.CostingPriceListStatus.Approved});
        await context.SaveChangesAsync();
        var result=await CreateRepository(context).CreateInvoice(new CreateInvoice {Code="INV-TEST",ProformaInvoiceId=ids.ProformaId,CustomerId=ids.CustomerId});
        Assert.Equal("Invoice.Price",result.Errors[0].Code);Assert.Empty(context.Invoices);
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
                Products = [new AllocateProductionOrderProduct { ProductId = ids.ProductId, ProductPackingId = ids.ProductPackingId, FulfilledQuantities=[new(){FinishedGoodsTransferNoteId=Guid.NewGuid(),Quantity=5}] }],
            },
            new ProformaInvoice
            {
                Id = ids.ProformaId, Code = "PI-1", AllocateProductionOrderId = allocationId, Approved = true, CustomerPoVerifiedAt = Now, CustomerPoAttachmentId = Guid.NewGuid(),
                Products = [new ProformaInvoiceProduct { Id = Guid.NewGuid(), ProductId = ids.ProductId, Quantity = 5 }],
            });
        var actor = new User { Id = ActorId, UserName = "Finance" }; var role = new Role { Id = Guid.NewGuid(), Name = "Finance" };
        context.AddRange(actor, role); context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = actor.Id, RoleId = role.Id });
        var index = 0;
        foreach (var key in new[] { PermissionKeys.CanCreateInvoice, PermissionKeys.CanGenerateProformaInvoice }) {
            context.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = ++index, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key });
            context.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = index, Key = key, Type = "Edit" });
        }
        return ids;
    }

    private static async Task FreezeProforma(ApplicationDbContext context, TestIds ids, decimal unitPrice, Guid currencyId)
    {
        // Fixture: the upstream proforma and customer PO already froze this agreed pack price.
        var line = await context.ProformaInvoiceProducts.SingleAsync(p => p.ProformaInvoiceId == ids.ProformaId);
        line.ProductPackingId = ids.ProductPackingId; line.UnitPrice = unitPrice; line.PriceCurrencyId = currencyId;
        await context.SaveChangesAsync();
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

    private static readonly Guid ActorId = Guid.Parse("00000000-0000-0000-0000-000000000789");
    private sealed class TestCurrentUser : ICurrentUserService
    {
        public Guid? UserId => ActorId;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
