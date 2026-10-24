using APP.Repository;
using APP.Services.Materials;
using DOMAIN.Entities.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

public class SupplierPurchaseUnitSyncTests
{
    [Fact]
    public async Task SupplierChangeProposesEquivalentPriceAndSynchronizesOnlyAfterApproval()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var data = new PurchaseUnitTestData(); await data.Seed(context);
        var current = data.Agreement(); context.Add(current); await context.SaveChangesAsync();
        var result = await SupplierPurchaseUnitSync.Request(context, PurchaseUnitTestData.Approvals(context),
            data.SupplierId, data.MaterialId, "g", data.Maker);
        Assert.True(result.IsSuccess);
        var proposal = await context.SupplierPricingAgreements.SingleAsync(row => row.ReplacesAgreementId == current.Id);
        Assert.Equal(0.02m, proposal.AgreedPrice);
        Assert.Equal("kg", (await context.SupplierManufacturers.SingleAsync()).PurchaseUnit);
        Assert.Equal(20m, current.AgreedPrice);
        var read = await SupplierPurchaseUnitReadModel.Apply(context, data.SupplierId,
            [new() { Material = new() { Id = data.MaterialId }, PurchaseUnit = "kg" }]);
        Assert.Equal("g", read[0].PendingPurchaseUnit);
        Assert.True((await SupplierPricingAgreementApprovalHandler.ApproveAsync(context, proposal.Id,
            data.Checker, [], "Reviewed conversion")).IsSuccess);
        Assert.Equal("g", (await context.SupplierManufacturers.SingleAsync()).PurchaseUnit);
        Assert.Equal(data.UnitId, (await context.SupplierManufacturers.SingleAsync()).UoMId);
        Assert.Equal(20m, current.AgreedPrice);
        Assert.NotEmpty(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task QuotedPriceDrivesProposalAndRejectionKeepsPriorUnit()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var data = new PurchaseUnitTestData(); await data.Seed(context);
        var current = data.Agreement(); context.Add(current); await context.SaveChangesAsync();
        var result = await SupplierPurchaseUnitSync.Request(context, PurchaseUnitTestData.Approvals(context),
            data.SupplierId, data.MaterialId, "g", data.Maker, 0.03m);
        Assert.True(result.IsSuccess);
        var proposal = context.SupplierPricingAgreements.Single(row => row.Status == SupplierPricingAgreementStatus.Pending);
        Assert.Equal(0.03m, proposal.AgreedPrice);
        Assert.True((await SupplierPricingAgreementApprovalHandler.RejectAsync(context, proposal.Id,
            data.Checker, [], "Not accepted")).IsSuccess);
        Assert.Equal("kg", context.SupplierManufacturers.Single().PurchaseUnit);
        Assert.Null(current.EffectiveTo);
    }

    [Fact]
    public async Task RepeatedSameChangeIsIdempotentAndConflictingPendingChangeIsRefused()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var data = new PurchaseUnitTestData(); await data.Seed(context);
        context.Add(data.Agreement()); await context.SaveChangesAsync();
        var approvals = PurchaseUnitTestData.Approvals(context);
        Assert.True((await SupplierPurchaseUnitSync.Request(context, approvals, data.SupplierId, data.MaterialId, "g", data.Maker)).IsSuccess);
        Assert.True((await SupplierPurchaseUnitSync.Request(context, approvals, data.SupplierId, data.MaterialId, "g", data.Maker)).IsSuccess);
        Assert.False((await SupplierPurchaseUnitSync.Request(context, approvals, data.SupplierId, data.MaterialId, "mg", data.Maker)).IsSuccess);
        Assert.Single(context.SupplierPricingAgreements, row => row.Status == SupplierPricingAgreementStatus.Pending);
    }

    [Fact]
    public async Task ContractPermissionIsRequiredWhenSupplierEditWouldChangeStandingPricing()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var data = new PurchaseUnitTestData(); await data.Seed(context, permission: false);
        context.Add(data.Agreement()); await context.SaveChangesAsync();
        var result = await SupplierPurchaseUnitSync.Request(context, PurchaseUnitTestData.Approvals(context),
            data.SupplierId, data.MaterialId, "g", data.Maker);
        Assert.False(result.IsSuccess);
        Assert.Equal("Supplier.PurchaseUnit.Permission", result.Error.Code);
        Assert.Single(context.SupplierPricingAgreements);
        Assert.Equal("kg", context.SupplierManufacturers.Single().PurchaseUnit);
    }

    [Fact]
    public async Task UnconvertibleStandingUnitIsRefusedAndNoUnitOrPriceChanges()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var data = new PurchaseUnitTestData(); await data.Seed(context);
        context.Add(data.Agreement()); await context.SaveChangesAsync();
        Assert.False((await SupplierPurchaseUnitSync.Request(context, PurchaseUnitTestData.Approvals(context),
            data.SupplierId, data.MaterialId, "L", data.Maker)).IsSuccess);
        Assert.Single(context.SupplierPricingAgreements);
        Assert.Equal("kg", context.SupplierManufacturers.Single().PurchaseUnit);
    }

    [Fact]
    public async Task PricingAgreementRevisionUpdatesSupplierAndFutureAgreementUsesEffectiveRead()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var data = new PurchaseUnitTestData(); await data.Seed(context, workflow: false);
        var current = data.Agreement(); context.Add(current); await context.SaveChangesAsync();
        var request = new SupplierPricingAgreementRequest { MaterialId = data.MaterialId, UoMId = data.UnitId,
            AgreedPrice = 0.025m, PriceUoM = "g", CurrencyId = data.CurrencyId, EffectiveFrom = DateTime.UtcNow.Date };
        Assert.True((await SupplierRelationshipTestContext.CreateRepository(context)
            .UpdatePricingAgreement(data.SupplierId, current.Id, request, data.Maker)).IsSuccess);
        Assert.Equal("g", context.SupplierManufacturers.Single().PurchaseUnit);
        var approved = context.SupplierPricingAgreements.Single(row => row.PriceUoM == "g");
        request.PriceUoM = "mg"; request.AgreedPrice = 0.000025m; request.EffectiveFrom = DateTime.UtcNow.Date.AddDays(2);
        Assert.True((await SupplierRelationshipTestContext.CreateRepository(context)
            .UpdatePricingAgreement(data.SupplierId, approved.Id, request, data.Maker)).IsSuccess);
        Assert.Equal("g", context.SupplierManufacturers.Single().PurchaseUnit);
    }
}
