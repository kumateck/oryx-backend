using System.ComponentModel.DataAnnotations;
using APP.Repository;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Procurement.Suppliers;
using Xunit;

namespace APP.Tests.Repository;
public partial class SupplierPricingTests
{
    [Theory]
    [InlineData("0.0001", true)]
    [InlineData("0.009", true)]
    [InlineData("0.01", true)]
    [InlineData("0", false)]
    [InlineData("-0.001", false)]
    public void ApiValidation_AcceptsPositiveSubCentPrices_RejectsNonPositive(string text, bool expected)
    {
        var price = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        var request = new SupplierPricingAgreementRequest { AgreedPrice = price };
        var validation = new ValidationContext(request) { MemberName = nameof(request.AgreedPrice) };
        Assert.Equal(expected, Validator.TryValidateProperty(price, validation, []));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PositiveSubCentPrice_UsesConfiguredApprovalRule(bool configured)
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var ids = SeedReferenceData(context); var reviewer = Guid.NewGuid(); var maker = Guid.NewGuid();
        if (configured) context.Approvals.Add(new Approval {
            Id = Guid.NewGuid(), ItemType = nameof(SupplierPricingAgreement),
            ApprovalStages = [new ApprovalStage { Id = Guid.NewGuid(), Order = 1, Required = true, UserId = reviewer }],
        });
        await context.SaveChangesAsync();
        var result = await SupplierRelationshipTestContext.CreateRepository(context)
            .CreatePricingAgreement(ids.SupplierId, Request(ids, 0.0001m), maker);
        Assert.True(result.IsSuccess);
        var agreement = await context.SupplierPricingAgreements.FindAsync(result.Value);
        Assert.Equal(0.0001m, agreement!.AgreedPrice);
        Assert.Equal(configured ? SupplierPricingAgreementStatus.Pending : SupplierPricingAgreementStatus.Approved, agreement.Status);
        if (configured)
        {
            var stage = Assert.Single(context.SupplierPricingAgreementApprovals);
            Assert.Equal(reviewer, stage.UserId); Assert.NotNull(stage.ActivatedAt);
            Assert.False((await SupplierPricingAgreementApprovalHandler.ApproveAsync(context, agreement.Id, maker, [], "Maker attempt")).IsSuccess);
            Assert.True((await SupplierPricingAgreementApprovalHandler.ApproveAsync(context, agreement.Id, reviewer, [], "Reviewed small unit price")).IsSuccess);
            Assert.Equal(SupplierPricingAgreementStatus.Approved, agreement.Status);
        }
        else Assert.Contains(context.ApprovalActionLogs, item => item.ModelId == agreement.Id && item.Status == ApprovalStatus.Approved);
    }
}
