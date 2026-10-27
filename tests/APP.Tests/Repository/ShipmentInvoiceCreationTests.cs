using DOMAIN.Entities.PurchaseOrders;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

[CollectionDefinition("Shipment invoice creation", DisableParallelization = true)]
public class ShipmentInvoiceCreationCollection;

[Collection("Shipment invoice creation")]
public class ShipmentInvoiceCreationTests
{
    [Fact]
    public async Task Supplied_quantities_save_without_overflow_or_cross_order_material_collisions()
    {
        await using var fixture = new ShipmentInvoiceCreationFixture();
        await fixture.Seed();
        var result = await fixture.Repository.CreateShipmentInvoice(fixture.Request,
            ShipmentInvoiceCreationFixture.ActorId);
        Assert.True(result.IsSuccess);
        var invoice = await fixture.Context.ShipmentInvoices.Include(i => i.Items).SingleAsync();
        Assert.Equal(3, invoice.Items.Count);
        Assert.Equal(fixture.Request.Items.Select(i => i.ReceivedQuantity).Order(),
            invoice.Items.Select(i => i.ReceivedQuantity).Order());
        Assert.All(invoice.Items, item => {
            Assert.Equal(1.5m, item.Price);
            Assert.Equal("kg", item.PriceUoM);
        });
        Assert.Equal(ShipmentInvoiceCreationFixture.ActorId, invoice.CreatedById);
        Assert.All(fixture.Orders, order => Assert.Equal(PurchaseOrderStatus.Linked, order.Status));
    }

    [Fact]
    public async Task Legacy_price_units_use_only_the_awarded_supplier_and_keep_purchase_order_prices_untouched()
    {
        await using var fixture = new ShipmentInvoiceCreationFixture();
        await fixture.Seed(legacy: true);
        foreach (var order in fixture.Orders)
        {
            await fixture.AddQuotation(order, winner: false, unit: "g");
            await fixture.AddQuotation(order);
        }
        var result = await fixture.Repository.CreateShipmentInvoice(fixture.Request,
            ShipmentInvoiceCreationFixture.ActorId);
        Assert.True(result.IsSuccess);
        var items = await fixture.Context.ShipmentInvoiceItems.ToListAsync();
        Assert.All(items, item => Assert.Equal("kg", item.PriceUoM));
        Assert.All(fixture.Orders, order => Assert.Null(order.Items[0].PriceUoM));
        Assert.True(ApplicationDbContext.EnforcePriceUoM);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unresolved_price_unit_returns_validation_without_any_invoice_or_order_mutation(bool competitorExists)
    {
        await using var fixture = new ShipmentInvoiceCreationFixture();
        await fixture.Seed(legacy: true);
        // Make the last line fail, after two resolvable lines have been prepared.
        foreach (var order in fixture.Orders.Take(2)) await fixture.AddQuotation(order);
        if (competitorExists) await fixture.AddQuotation(fixture.Orders[2], winner: false);
        var result = await fixture.Repository.CreateShipmentInvoice(fixture.Request,
            ShipmentInvoiceCreationFixture.ActorId);
        Assert.False(result.IsSuccess);
        Assert.Equal("Item.PriceUoM", result.Error.Code);
        await AssertNoMutation(fixture);
    }

    [Fact]
    public async Task Persisted_price_unit_and_order_price_win_over_quotation_and_request()
    {
        await using var fixture = new ShipmentInvoiceCreationFixture();
        await fixture.Seed();
        foreach (var order in fixture.Orders) await fixture.AddQuotation(order, unit: "g");
        foreach (var item in fixture.Request.Items) { item.Price = 999m; item.PriceUoM = "L"; }
        var result = await fixture.Repository.CreateShipmentInvoice(fixture.Request,
            ShipmentInvoiceCreationFixture.ActorId);
        Assert.True(result.IsSuccess);
        Assert.All(await fixture.Context.ShipmentInvoiceItems.ToListAsync(), item => {
            Assert.Equal(1.5m, item.Price);
            Assert.Equal("kg", item.PriceUoM);
        });
    }

    [Fact]
    public async Task Over_receipt_on_last_line_does_not_mutate_earlier_orders()
    {
        await using var fixture = new ShipmentInvoiceCreationFixture();
        await fixture.Seed();
        fixture.Request.Items[2].ReceivedQuantity++;
        var result = await fixture.Repository.CreateShipmentInvoice(fixture.Request,
            ShipmentInvoiceCreationFixture.ActorId);
        Assert.False(result.IsSuccess);
        Assert.Equal("Item.ReceivedQuantity", result.Error.Code);
        await AssertNoMutation(fixture);
    }

    [Fact]
    public async Task Partial_receipt_keeps_the_remaining_purchase_order_quantity_open()
    {
        await using var fixture = new ShipmentInvoiceCreationFixture();
        await fixture.Seed();
        fixture.Request.Items[0].ReceivedQuantity /= 2;
        var result = await fixture.Repository.CreateShipmentInvoice(fixture.Request,
            ShipmentInvoiceCreationFixture.ActorId);
        Assert.True(result.IsSuccess);
        Assert.Equal(PurchaseOrderStatus.PartiallyLinked, fixture.Orders[0].Status);
        Assert.Equal(6_000_000_000m, fixture.Orders[0].Items[0].QuantityInvoiced);
    }

    private static async Task AssertNoMutation(ShipmentInvoiceCreationFixture fixture)
    {
        Assert.Empty(await fixture.Context.ShipmentInvoices.AsNoTracking().ToListAsync());
        Assert.All(fixture.Orders, order => {
            Assert.Equal(0m, order.Items[0].QuantityInvoiced);
            Assert.Equal(PurchaseOrderStatus.Completed, order.Status);
        });
        Assert.All(fixture.Context.ChangeTracker.Entries(), entry =>
            Assert.Equal(EntityState.Unchanged, entry.State));
    }
}
