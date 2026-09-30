using APP.Repository;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

public class SupplierPricingTests
{
    private static readonly DateTime Boundary = new(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ActiveAgreement_IsInclusiveAtEffectiveToBoundary()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var ids = SeedReferenceData(context);
        context.SupplierPricingAgreements.Add(Agreement(ids, Boundary.AddMonths(-1), Boundary, 12m));
        await context.SaveChangesAsync();
        var repository = SupplierRelationshipTestContext.CreateRepository(context);

        var result = await repository.GetActivePricingAgreement(
            ids.SupplierId, ids.MaterialId, ids.UomId, Boundary);

        Assert.True(result.IsSuccess);
        Assert.Equal(12m, result.Value.AgreedPrice);
    }

    [Fact]
    public async Task ActiveAgreement_ReturnsConflictWhenLegacyDataIsAmbiguous()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var ids = SeedReferenceData(context);
        context.SupplierPricingAgreements.AddRange(
            Agreement(ids, Boundary.AddMonths(-2), null, 12m),
            Agreement(ids, Boundary.AddMonths(-1), null, 14m)
        );
        await context.SaveChangesAsync();
        var repository = SupplierRelationshipTestContext.CreateRepository(context);

        var result = await repository.GetActivePricingAgreement(
            ids.SupplierId, ids.MaterialId, ids.UomId, Boundary);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CreateWithoutWorkflow_AutoApprovesAndWritesAuditLog()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var ids = SeedReferenceData(context);
        await context.SaveChangesAsync();
        var result = await SupplierRelationshipTestContext.CreateRepository(context)
            .CreatePricingAgreement(ids.SupplierId, Request(ids, 20m), Guid.NewGuid());

        Assert.True(result.IsSuccess);
        var agreement = await context.SupplierPricingAgreements.FindAsync(result.Value);
        Assert.Equal(SupplierPricingAgreementStatus.Approved, agreement.Status);
        Assert.Contains(context.ApprovalActionLogs, log => log.ModelId == result.Value
            && log.Status == ApprovalStatus.Approved);
    }

    [Fact]
    public async Task EmptyWorkflow_AutoApprovesWithSystemAudit()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var ids = SeedReferenceData(context);
        context.Approvals.Add(new Approval {
            Id = Guid.NewGuid(), ItemType = nameof(SupplierPricingAgreement),
            ApprovalStages = [],
        });
        await context.SaveChangesAsync();
        var result = await SupplierRelationshipTestContext.CreateRepository(context)
            .CreatePricingAgreement(ids.SupplierId, Request(ids, 20m), Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(SupplierPricingAgreementStatus.Approved,
            (await context.SupplierPricingAgreements.FindAsync(result.Value)).Status);
        Assert.Contains(context.ApprovalActionLogs, log => log.ModelId == result.Value
            && log.Status == ApprovalStatus.Approved && log.UserId == null);
    }

    [Fact]
    public async Task ConfiguredWorkflow_KeepsProposalInactiveUntilAssignedApproval()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var ids = SeedReferenceData(context);
        var approverId = Guid.NewGuid();
        context.Approvals.Add(new Approval {
            Id = Guid.NewGuid(), ItemType = nameof(SupplierPricingAgreement),
            ApprovalStages = [new ApprovalStage { Id = Guid.NewGuid(), Order = 1,
                Required = true, UserId = approverId }],
        });
        await context.SaveChangesAsync();
        var proposerId = Guid.NewGuid();
        var repository = SupplierRelationshipTestContext.CreateRepository(context);
        var created = await repository.CreatePricingAgreement(ids.SupplierId, Request(ids, 20m), proposerId);
        Assert.True(created.IsSuccess);
        Assert.Equal(SupplierPricingAgreementStatus.Pending,
            (await context.SupplierPricingAgreements.FindAsync(created.Value)).Status);
        Assert.False((await repository.GetActivePricingAgreement(ids.SupplierId, ids.MaterialId,
            ids.UomId, Boundary)).IsSuccess);

        var makerReview = await SupplierPricingAgreementApprovalHandler.ApproveAsync(
            context, created.Value, proposerId, [], null);
        Assert.False(makerReview.IsSuccess);
        var approved = await SupplierPricingAgreementApprovalHandler.ApproveAsync(
            context, created.Value, approverId, [], "Reviewed terms");
        Assert.True(approved.IsSuccess);
        Assert.Equal(20m, (await repository.GetActivePricingAgreement(ids.SupplierId,
            ids.MaterialId, ids.UomId, Boundary)).Value.AgreedPrice);
    }

    [Fact]
    public async Task ApprovedRevision_ClosesPriorWindowOnlyAfterApproval()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var ids = SeedReferenceData(context);
        var prior = Agreement(ids, Boundary.AddMonths(-2), null, 12m);
        context.SupplierPricingAgreements.Add(prior);
        var approverId = Guid.NewGuid();
        context.Approvals.Add(new Approval {
            Id = Guid.NewGuid(), ItemType = nameof(SupplierPricingAgreement),
            ApprovalStages = [new ApprovalStage { Id = Guid.NewGuid(), Order = 1,
                Required = true, UserId = approverId }],
        });
        await context.SaveChangesAsync();
        var repository = SupplierRelationshipTestContext.CreateRepository(context);
        var request = Request(ids, 20m);
        var revised = await repository.UpdatePricingAgreement(ids.SupplierId, prior.Id,
            request, Guid.NewGuid());
        Assert.True(revised.IsSuccess);
        Assert.Equal(12m, (await repository.GetActivePricingAgreement(ids.SupplierId,
            ids.MaterialId, ids.UomId, Boundary)).Value.AgreedPrice);

        var proposal = context.SupplierPricingAgreements.Single(item =>
            item.ReplacesAgreementId == prior.Id);
        var approved = await SupplierPricingAgreementApprovalHandler.ApproveAsync(
            context, proposal.Id, approverId, [], "Price checked");
        Assert.True(approved.IsSuccess);
        Assert.Equal(request.EffectiveFrom.Date.AddMilliseconds(-1), prior.EffectiveTo);
        Assert.Equal(20m, (await repository.GetActivePricingAgreement(ids.SupplierId,
            ids.MaterialId, ids.UomId, Boundary)).Value.AgreedPrice);
    }

    [Fact]
    public async Task ArchiveWithoutWorkflow_AutoApprovesButKeepsProposalAuditRecord()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var ids = SeedReferenceData(context);
        var prior = Agreement(ids, Boundary.AddMonths(-1), null, 12m);
        context.SupplierPricingAgreements.Add(prior);
        await context.SaveChangesAsync();
        var repository = SupplierRelationshipTestContext.CreateRepository(context);
        var result = await repository.DeletePricingAgreement(ids.SupplierId, prior.Id, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.NotNull(prior.DeletedAt);
        var proposal = context.SupplierPricingAgreements.IgnoreQueryFilters()
            .Single(item => item.ReplacesAgreementId == prior.Id);
        Assert.Equal(SupplierPricingAgreementStatus.Approved, proposal.Status);
        Assert.Null(proposal.DeletedAt);
        Assert.Contains(context.ApprovalActionLogs, log => log.ModelId == proposal.Id
            && log.Status == ApprovalStatus.Approved);
        Assert.True((await repository.CreatePricingAgreement(ids.SupplierId,
            Request(ids, 15m), Guid.NewGuid())).IsSuccess);
    }

    [Fact]
    public async Task RejectedRevision_LeavesCurrentAgreementEffective()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var ids = SeedReferenceData(context);
        var prior = Agreement(ids, Boundary.AddMonths(-2), null, 12m);
        context.SupplierPricingAgreements.Add(prior);
        var approverId = Guid.NewGuid();
        context.Approvals.Add(new Approval {
            Id = Guid.NewGuid(), ItemType = nameof(SupplierPricingAgreement),
            ApprovalStages = [new ApprovalStage { Id = Guid.NewGuid(), Order = 1,
                Required = true, UserId = approverId }],
        });
        await context.SaveChangesAsync();
        var repository = SupplierRelationshipTestContext.CreateRepository(context);
        Assert.True((await repository.UpdatePricingAgreement(ids.SupplierId, prior.Id,
            Request(ids, 20m), Guid.NewGuid())).IsSuccess);
        var proposal = context.SupplierPricingAgreements.Single(item =>
            item.ReplacesAgreementId == prior.Id);

        var rejected = await SupplierPricingAgreementApprovalHandler.RejectAsync(
            context, proposal.Id, approverId, [], "Price not accepted");
        Assert.True(rejected.IsSuccess);
        Assert.Equal(SupplierPricingAgreementStatus.Rejected, proposal.Status);
        Assert.Null(prior.EffectiveTo);
        Assert.Equal(12m, (await repository.GetActivePricingAgreement(ids.SupplierId,
            ids.MaterialId, ids.UomId, Boundary)).Value.AgreedPrice);
    }

    private static SupplierPricingAgreementRequest Request(PricingIds ids, decimal price) => new()
    {
        MaterialId = ids.MaterialId, UoMId = ids.UomId, CurrencyId = ids.CurrencyId,
        AgreedPrice = price, PriceUoM = "kg", EffectiveFrom = Boundary.AddMonths(-1),
    };

    private static PricingIds SeedReferenceData(INFRASTRUCTURE.Context.ApplicationDbContext context)
    {
        var ids = new PricingIds();
        var manufacturerId = Guid.NewGuid();
        context.AddRange(
            new Supplier { Id = ids.SupplierId, Name = "Supplier" },
            new Manufacturer { Id = manufacturerId, Name = "Manufacturer" },
            new SupplierManufacturer { Id = Guid.NewGuid(), SupplierId = ids.SupplierId,
                ManufacturerId = manufacturerId, MaterialId = ids.MaterialId, UoMId = ids.UomId },
            new Material { Id = ids.MaterialId, Name = "Material" },
            new UnitOfMeasure { Id = ids.UomId, Name = "Kilogram", Symbol = "kg" },
            new Currency { Id = ids.CurrencyId, Name = "US Dollar", Symbol = "$" }
        );
        return ids;
    }

    private static SupplierPricingAgreement Agreement(
        PricingIds ids, DateTime from, DateTime? to, decimal price) => new()
    {
        Id = Guid.NewGuid(), SupplierId = ids.SupplierId, MaterialId = ids.MaterialId,
        UoMId = ids.UomId, CurrencyId = ids.CurrencyId, AgreedPrice = price,
        PriceUoM = "kg", EffectiveFrom = from, EffectiveTo = to,
    };

    private sealed class PricingIds
    {
        public Guid SupplierId { get; } = Guid.NewGuid();
        public Guid MaterialId { get; } = Guid.NewGuid();
        public Guid UomId { get; } = Guid.NewGuid();
        public Guid CurrencyId { get; } = Guid.NewGuid();
    }
}
