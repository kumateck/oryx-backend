using APP.Services.Materials;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Requisitions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

public class SupplierPurchaseUnitWriteTests
{
    [Fact]
    public async Task SymbolOnlySupplierCreateAndEditRoundTripWithoutChangingQuantityReference()
    {
        var data = new PurchaseUnitTestData();
        await using var context = SupplierRelationshipTestContext.Create(data.Maker);
        await data.Seed(context);
        var department = new Department { Id = Guid.NewGuid(), Name = "Other department" };
        context.Add(department); context.Add(data.Link(department.Id, data.UnitId));
        await context.SaveChangesAsync();
        var request = Request(data, " g "); request.Name = "New supplier";
        var repository = PurchaseUnitTestData.Procurement(context);
        var created = await repository.CreateSupplier(request, data.Maker);
        Assert.True(created.IsSuccess);
        var associations = await repository.GetSupplierManufacturersBySupplier(created.Value);
        Assert.Equal("g", Assert.Single(associations.Value).PurchaseUnit);
        Assert.Equal(data.UnitId, associations.Value[0].UoM.Id);
        request.AssociatedManufacturers[0].PurchaseUnit = "KG";
        Assert.True((await repository.UpdateSupplier(request, created.Value, data.Maker)).IsSuccess);
        context.ChangeTracker.Clear();
        Assert.Equal("kg", Assert.Single((await repository.GetSupplierManufacturersBySupplier(created.Value)).Value).PurchaseUnit);
        Assert.Equal("kg", Assert.Single((await repository.GetSupplier(created.Value)).Value.AssociatedManufacturers).PurchaseUnit);
    }

    [Fact]
    public async Task PendingUnitChangeDoesNotBlockUnchangedSupplierEdit()
    {
        var data = new PurchaseUnitTestData();
        await using var context = SupplierRelationshipTestContext.Create(data.Maker);
        await data.Seed(context);
        context.Add(data.Agreement()); await context.SaveChangesAsync();
        var approvals = PurchaseUnitTestData.Approvals(context);
        Assert.True((await SupplierPurchaseUnitSync.Request(context, approvals, data.SupplierId, data.MaterialId, "g", data.Maker)).IsSuccess);
        Assert.True((await PurchaseUnitTestData.Procurement(context).UpdateSupplier(Request(data, "kg"), data.SupplierId, data.Maker)).IsSuccess);
        Assert.Equal("kg", context.SupplierManufacturers.Single().PurchaseUnit);
        Assert.Single(context.SupplierPricingAgreements, row => row.Status == SupplierPricingAgreementStatus.Pending);
    }

    [Fact]
    public async Task QuotationUnitChangeProposesStandingPriceAndPreservesHistoricalQuotation()
    {
        var data = new PurchaseUnitTestData();
        await using var context = SupplierRelationshipTestContext.Create(data.Maker);
        await data.Seed(context);
        context.Add(data.Agreement());
        var quotation = new SupplierQuotation { Id = Guid.NewGuid(), SupplierId = data.SupplierId };
        var open = new SupplierQuotationItem { Id = Guid.NewGuid(), SupplierQuotationId = quotation.Id,
            MaterialId = data.MaterialId, UoMId = data.UnitId, Quantity = 1000 };
        var historical = new SupplierQuotationItem { Id = Guid.NewGuid(), SupplierQuotationId = quotation.Id,
            MaterialId = data.MaterialId, UoMId = data.UnitId, QuotedPrice = 20, PriceUoM = "kg",
            Status = SupplierQuotationItemStatus.Processed };
        context.AddRange(quotation, open, historical); await context.SaveChangesAsync();
        var recorder = new SupplierQuotationPriceRecorder(context, PurchaseUnitTestData.Approvals(context));
        var result = await recorder.Record([new() { Id = open.Id, Price = 0.03m, PriceUoM = " g " }], quotation.Id, data.Maker);
        Assert.True(result.IsSuccess);
        Assert.Equal("g", open.PriceUoM); Assert.Equal(0.03m, open.QuotedPrice);
        Assert.Equal(data.Maker, open.LastUpdatedById);
        Assert.Equal("kg", historical.PriceUoM); Assert.Equal(20m, historical.QuotedPrice);
        Assert.Equal("kg", context.SupplierManufacturers.Single().PurchaseUnit);
        var proposal = context.SupplierPricingAgreements.Single(row => row.Status == SupplierPricingAgreementStatus.Pending);
        Assert.Equal("g", proposal.PriceUoM); Assert.Equal(0.03m, proposal.AgreedPrice);
        Assert.False((await recorder.Record([new() { Id = historical.Id, Price = 1, PriceUoM = "g" }], quotation.Id, data.Maker)).IsSuccess);
        Assert.Equal("kg", historical.PriceUoM);
    }

    private static CreateSupplierRequest Request(PurchaseUnitTestData data, string symbol) => new() {
        Name = "Supplier", CurrencyId = data.CurrencyId,
        AssociatedManufacturers = [new() { MaterialId = data.MaterialId, ManufacturerId = data.ManufacturerId,
            PurchaseUnit = symbol, Default = true }],
    };
}
