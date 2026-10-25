using APP.Services.Procurement;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Shipments.Request;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Procurement.Manufacturers;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class ShipmentShortageWorkflowTests
{
    [Fact]
    public async Task ApprovedShortageCreatesOnlyNoChargeInvoiceAndReservesQuantity()
    {
        await using var context = NewContext();
        var line = Seed(context, declared: 10, physical: 7, approved: true);
        var request = new CreateShortageMakeUpInvoice
        {
            Code = "MAKEUP-1", ShipmentDiscrepancyItemId = line.Id,
            Quantity = 2, RequestKey = Guid.NewGuid(),
        };
        var first = await ShortageMakeUpInvoiceCreation.Create(context, request, Guid.NewGuid());
        Assert.True(first.IsSuccess, first.IsFailure ? first.Error.ToString() : "");
        var again = await ShortageMakeUpInvoiceCreation.Create(context, request, Guid.NewGuid());
        Assert.True(again.IsSuccess, again.IsFailure ? again.Error.ToString() : "");
        Assert.Equal(first.Value, again.Value);
        var invoice = await context.ShipmentInvoices.Include(item => item.Items)
            .SingleAsync(item => item.Id == first.Value);
        Assert.Equal(ShipmentInvoiceKind.NoChargeShortageMakeUp, invoice.Kind);
        Assert.Equal(0, invoice.TotalCost);
        Assert.Equal(0, invoice.Items.Single().Price);
        Assert.Equal(line.Id, invoice.Items.Single().FulfillsDiscrepancyItemId);
        var balance = await ShipmentShortageBalance.Load(context, line.Id);
        Assert.Equal(3, balance.OutstandingQuantity);
        Assert.Equal(2, balance.InFlightMakeUpQuantity);
        Assert.Equal(1, balance.AvailableToShipQuantity);
        var excess = await ShortageMakeUpInvoiceCreation.Create(context,
            new CreateShortageMakeUpInvoice { Code = "MAKEUP-2",
                ShipmentDiscrepancyItemId = line.Id, Quantity = 2,
                RequestKey = Guid.NewGuid() }, Guid.NewGuid());
        Assert.False(excess.IsSuccess);
    }

    [Fact]
    public async Task UnapprovedCaseCannotCreateMakeUpInvoice()
    {
        await using var context = NewContext();
        var line = Seed(context, 10, 7, approved: false);
        var result = await ShortageMakeUpInvoiceCreation.Create(context,
            new CreateShortageMakeUpInvoice { Code = "MAKEUP-3",
                ShipmentDiscrepancyItemId = line.Id, Quantity = 1,
                RequestKey = Guid.NewGuid() }, Guid.NewGuid());
        Assert.False(result.IsSuccess);
        Assert.Empty(context.ShipmentInvoices.Where(item =>
            item.Kind == ShipmentInvoiceKind.NoChargeShortageMakeUp));
    }

    private static ApplicationDbContext NewContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());

    private static ShipmentDiscrepancyItem Seed(ApplicationDbContext context,
        decimal declared, decimal physical, bool approved)
    {
        var invoice = new ShipmentInvoice { Id = Guid.NewGuid(), Code = "ORIGINAL",
            SupplierId = Guid.NewGuid(), TotalCost = 100 };
        var source = new ShipmentInvoiceItem { Id = Guid.NewGuid(),
            ShipmentInvoiceId = invoice.Id, ShipmentInvoice = invoice,
            MaterialId = Guid.NewGuid(), UoMId = Guid.NewGuid(),
            ManufacturerId = Guid.NewGuid(), PurchaseOrderId = Guid.NewGuid(),
            ReceivedQuantity = declared, Price = 10, PriceUoM = "kg" };
        source.Material = new Material { Id = source.MaterialId, Name = "Sample material" };
        source.UoM = new UnitOfMeasure { Id = source.UoMId, Symbol = "kg" };
        source.Manufacturer = new Manufacturer { Id = source.ManufacturerId, Name = "Sample manufacturer" };
        var document = new ShipmentDocument { Id = Guid.NewGuid(), Code = "SHIP-1",
            ShipmentInvoiceId = invoice.Id };
        var report = new ShipmentDiscrepancy { Id = Guid.NewGuid(),
            ShipmentDocumentId = document.Id, Approved = approved };
        var line = new ShipmentDiscrepancyItem { Id = Guid.NewGuid(),
            ShipmentDiscrepancyId = report.Id, ShipmentInvoiceItemId = source.Id,
            DeclaredQuantitySnapshot = declared, ReceivedQuantity = physical };
        context.AddRange(invoice, source, document, report, line);
        context.SaveChanges();
        return line;
    }

    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
