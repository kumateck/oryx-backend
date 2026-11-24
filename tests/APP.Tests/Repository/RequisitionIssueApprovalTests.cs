using APP.Repository;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.BinCards;
using Microsoft.Extensions.Logging.Abstractions;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Requisitions.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class RequisitionIssueCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class RequisitionIssueApprovalTests
{
    [Fact]
    public async Task IssueStockRequisition_RejectsRequisitionAwaitingApproval()
    {
        await using var context = CreateContext();
        var requisition = CreateStockRequisition(approved: false);
        context.Requisitions.Add(requisition);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).IssueStockRequisition(
            requisition.Id,
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Requisition.ApprovalRequired", result.Error.Code);
        Assert.Equal(RequestStatus.New, requisition.Status);
    }

    [Fact]
    public async Task IssueStockRequisition_AllowsAutoApprovedRequisitionPastApprovalGuard()
    {
        await using var context = CreateContext();
        var requisition = CreateStockRequisition(approved: true);
        context.Requisitions.Add(requisition);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).IssueStockRequisition(
            requisition.Id,
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
        Assert.NotEqual("Requisition.ApprovalRequired", result.Error.Code);
    }

    [Fact]
    public async Task IssueStockRequisition_RejectsCompletedRequisitionBeforeMovingStock()
    {
        await using var context = CreateContext();
        var requisition = CreateStockRequisition(approved: true);
        requisition.Status = RequestStatus.Completed;
        context.Requisitions.Add(requisition);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).IssueStockRequisition(
            requisition.Id,
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Stock.AlreadyIssued", result.Error.Code);
        Assert.Empty(context.MaterialBatchEvents);
        Assert.Empty(context.BinCardInformation);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IssueStockRequisition_RejectsIssueEvidenceOnAPendingRequisition(bool useIssueRecord)
    {
        await using var context = CreateContext();
        var requisition = CreateStockRequisition(approved: true);
        context.Requisitions.Add(requisition);
        AddIssueEvidence(context, requisition, useIssueRecord);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).IssueStockRequisition(requisition.Id, Guid.NewGuid());

        Assert.Equal("Stock.AlreadyIssued", result.Error.Code);
        Assert.Equal(RequestStatus.Pending, requisition.Status);
        Assert.Equal(useIssueRecord ? 1 : 0, await context.StockRequisitionIssues.CountAsync());
        Assert.Equal(useIssueRecord ? 0 : 1, await context.BinCardInformation.CountAsync());
        Assert.Empty(context.MaterialBatchEvents);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ApproveStockRequisition_DoesNotReopenIssuedStock(bool pendingHeader)
    {
        await using var context = CreateContext();
        var actor = Guid.NewGuid();
        var requisition = CreateStockRequisition(approved: true);
        requisition.Status = pendingHeader ? RequestStatus.Pending : RequestStatus.Completed;
        var stage = new RequisitionApproval
        {
            Id = Guid.NewGuid(), UserId = actor, Required = true, ActivatedAt = DateTime.UtcNow,
        };
        requisition.Approvals = [stage];
        context.Requisitions.Add(requisition);
        AddIssueEvidence(context, requisition, useIssueRecord: true);
        await context.SaveChangesAsync();
        var repository = new ApprovalRepository(context, null!, null!, null!, NullLogger<ApprovalRepository>.Instance,
            null!, new NoOpProductionActivityStepEventPublisher());

        var result = await repository.ApproveItem("StockRequisition", requisition.Id, actor, []);

        Assert.Equal("StockRequisition.AlreadyIssued", result.Error.Code);
        Assert.Equal(pendingHeader ? RequestStatus.Pending : RequestStatus.Completed, requisition.Status);
        Assert.Equal(ApprovalStatus.Pending, stage.Status);
        Assert.Single(context.StockRequisitionIssues);
    }

    private static void AddIssueEvidence(ApplicationDbContext context, Requisition requisition, bool useIssueRecord)
    {
        if (useIssueRecord)
            context.StockRequisitionIssues.Add(new StockRequisitionIssue
            {
                Id = Guid.NewGuid(), RequisitionId = requisition.Id, IssuedById = Guid.NewGuid(), IssuedAt = DateTime.UtcNow,
            });
        else
            context.BinCardInformation.Add(new BinCardInformation
            {
                Id = Guid.NewGuid(), RequisitionId = requisition.Id, QuantityIssued = 10,
            });
    }

    [Fact]
    public async Task LegacyRequisitionApproval_CannotBypassConfiguredWorkflow()
    {
        await using var context = CreateContext();
        var requisition = CreateStockRequisition(approved: false);
        context.Requisitions.Add(requisition);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).ApproveRequisition(
            new ApproveRequisitionRequest(),
            requisition.Id,
            Guid.NewGuid(),
            [Guid.NewGuid()]
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Requisition.ApprovalWorkflowRequired", result.Error.Code);
        Assert.False(requisition.Approved);
    }

    private static Requisition CreateStockRequisition(bool approved) =>
        new()
        {
            Id = Guid.NewGuid(),
            Code = "SR-APPROVAL-001",
            RequestedById = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            RequisitionType = RequisitionType.Stock,
            Status = approved ? RequestStatus.Pending : RequestStatus.New,
            Approved = approved,
            ProductionScheduleProductId = Guid.NewGuid(),
            Items = [],
        };

    private static RequisitionRepository CreateRepository(ApplicationDbContext context) =>
        new(
            context,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            new NoOpProductionActivityStepEventPublisher()
        );

    private static ApplicationDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new RequisitionIssueCurrentUser()
        );
}
