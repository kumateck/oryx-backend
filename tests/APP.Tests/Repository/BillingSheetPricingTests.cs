using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Charges;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Shipments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class NoCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

/// <summary>
/// A billing sheet's invoice can reference a ShipmentInvoiceItem written before
/// Price/PriceUoM were frozen at issue (see PriceUoMPropagationTests). Without healing
/// those legacy rows, the print/pay-charges screens compute an invoice total of zero
/// against a real, priced purchase order.
/// </summary>
public class BillingSheetPricingTests
{
    private static ApplicationDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new NoCurrentUserService()
        );

    private static IMapper CreateMapper(ApplicationDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton(context);
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static ProcurementRepository CreateRepository(ApplicationDbContext context) =>
        new(
            context,
            CreateMapper(context),
            null!,
            null!,
            null!,
            null!,
            new PaymentRepository(context, CreateMapper(context))
        );

    private static async Task<(Guid InvoiceId, Guid BillingSheetId)> SeedLegacyBillingSheet(
        ApplicationDbContext context
    )
    {
        var supplierId = Guid.NewGuid();
        var currencyId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var uomId = Guid.NewGuid();
        var manufacturerId = Guid.NewGuid();
        var purchaseOrderId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var chargeId = Guid.NewGuid();
        var billingSheetId = Guid.NewGuid();

        context.Currencies.Add(new Currency { Id = currencyId, Name = "Cedi", Symbol = "GH₵" });
        context.Suppliers.Add(new Supplier { Id = supplierId, Name = "Supplier", CurrencyId = currencyId });
        context.Materials.Add(new Material { Id = materialId, Name = "Material" });
        context.UnitOfMeasures.Add(new UnitOfMeasure { Id = uomId, Symbol = "kg" });
        context.Manufacturers.Add(new Manufacturer { Id = manufacturerId, Name = "Manufacturer" });
        context.Charges.Add(new Charge { Id = chargeId, Name = "Shipping Line" });

        // Priced correctly on the purchase order.
        context.PurchaseOrders.Add(
            new PurchaseOrder
            {
                Id = purchaseOrderId,
                Code = "PO-1",
                SupplierId = supplierId,
                Items =
                [
                    new PurchaseOrderItem
                    {
                        Id = Guid.NewGuid(),
                        MaterialId = materialId,
                        UoMId = uomId,
                        Quantity = 900,
                        Price = 1.3m,
                        PriceUoM = "kg",
                    },
                ],
            }
        );

        // The invoice line predates the Price/PriceUoM freeze: both are unset, exactly
        // like a row written before the migration that added the columns.
        context.ShipmentInvoices.Add(
            new ShipmentInvoice
            {
                Id = invoiceId,
                Code = "INV-1",
                SupplierId = supplierId,
                Items =
                [
                    new ShipmentInvoiceItem
                    {
                        Id = Guid.NewGuid(),
                        MaterialId = materialId,
                        UoMId = uomId,
                        ManufacturerId = manufacturerId,
                        PurchaseOrderId = purchaseOrderId,
                        ExpectedQuantity = 900,
                        ReceivedQuantity = 900,
                        Price = 0,
                        PriceUoM = null,
                        TotalCost = 1170,
                    },
                ],
            }
        );

        context.BillingSheets.Add(
            new BillingSheet
            {
                Id = billingSheetId,
                Code = "BS-1",
                BillOfLading = "BOL-1",
                SupplierId = supplierId,
                InvoiceId = invoiceId,
                Charges =
                [
                    new BillingSheetCharge
                    {
                        Id = Guid.NewGuid(),
                        ChargeId = chargeId,
                        CurrencyId = currencyId,
                        Amount = 20,
                    },
                ],
            }
        );

        await context.SaveChangesAsync();

        return (invoiceId, billingSheetId);
    }

    [Fact]
    public async Task GetBillingSheetByInvoice_heals_a_legacy_invoice_line()
    {
        await using var context = CreateContext();
        var (invoiceId, _) = await SeedLegacyBillingSheet(context);
        var repository = CreateRepository(context);

        var result = await repository.GetBillingSheetByInvoice(invoiceId);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Invoice!.Items);
        Assert.Equal(1.3m, item.Price);
        Assert.Equal("kg", item.PriceUoM);
        var charge = Assert.Single(result.Value.Charges);
        Assert.Equal("GH₵", charge.Currency?.Symbol);
    }

    [Fact]
    public async Task GetBillingSheet_heals_a_legacy_invoice_line()
    {
        await using var context = CreateContext();
        var (_, billingSheetId) = await SeedLegacyBillingSheet(context);
        var repository = CreateRepository(context);

        var result = await repository.GetBillingSheet(billingSheetId);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Invoice!.Items);
        Assert.Equal(1.3m, item.Price);
        Assert.Equal("kg", item.PriceUoM);
        var charge = Assert.Single(result.Value.Charges);
        Assert.Equal("GH₵", charge.Currency?.Symbol);
    }

    [Fact]
    public async Task GetBillingSheetByInvoice_returns_the_currency_each_charge_was_saved_with()
    {
        await using var context = CreateContext();
        var (invoiceId, _) = await SeedLegacyBillingSheet(context);
        var repository = CreateRepository(context);

        var result = await repository.GetBillingSheetByInvoice(invoiceId);

        var charge = Assert.Single(result.Value.Charges);
        Assert.Equal("GH₵", charge.Currency?.Symbol);
    }
}
