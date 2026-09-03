using APP.Repository;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class RequisitionAutoApprovalCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class RequisitionAutoApprovalTests
{
    [Fact]
    public async Task CreateInitialApprovalsAsync_AutoApprovesPurchaseRequisition_WhenWorkflowIsMissing()
    {
        await using var context = CreateContext();
        var requisition = CreatePurchaseRequisition();
        context.Requisitions.Add(requisition);
        await context.SaveChangesAsync();
        var repository = CreateRepository(context);

        await repository.CreateInitialApprovalsAsync("PurchaseRequisition", requisition.Id);

        Assert.True(requisition.Approved);
        Assert.Equal(RequestStatus.Pending, requisition.Status);
        Assert.All(requisition.Items, item => Assert.Equal(RequestStatus.Pending, item.Status));
        Assert.Empty(context.RequisitionApprovals);
        var auditEvent = Assert.Single(context.ApprovalActionLogs);
        Assert.Equal(requisition.Id, auditEvent.ModelId);
        Assert.Equal(ApprovalStatus.Approved, auditEvent.Status);
        Assert.Null(auditEvent.UserId);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_PreservesConfiguredPurchaseApprovalWorkflow()
    {
        await using var context = CreateContext();
        var requisition = CreatePurchaseRequisition();
        var approval = new Approval
        {
            Id = Guid.NewGuid(),
            ItemType = "PurchaseRequisition",
            ApprovalStages = [],
        };
        approval.ApprovalStages.Add(
            new ApprovalStage
            {
                Id = Guid.NewGuid(),
                ApprovalId = approval.Id,
                Approval = approval,
                Order = 1,
                Required = true,
                UserId = Guid.NewGuid(),
            }
        );
        context.AddRange(requisition, approval);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(
            "PurchaseRequisition",
            requisition.Id
        );

        Assert.False(requisition.Approved);
        Assert.Equal(RequestStatus.New, requisition.Status);
        Assert.All(requisition.Items, item => Assert.Equal(RequestStatus.New, item.Status));
        var pendingApproval = Assert.Single(context.RequisitionApprovals);
        Assert.Equal(ApprovalStatus.Pending, pendingApproval.Status);
        Assert.NotNull(pendingApproval.ActivatedAt);
        Assert.Empty(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_AutoApprovesPurchaseRequisition_WhenWorkflowHasNoStages()
    {
        await using var context = CreateContext();
        var requisition = CreatePurchaseRequisition();
        context.AddRange(
            requisition,
            new Approval
            {
                Id = Guid.NewGuid(),
                ItemType = "PurchaseRequisition",
                ApprovalStages = [],
            }
        );
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(
            "PurchaseRequisition",
            requisition.Id
        );

        Assert.True(requisition.Approved);
        Assert.Equal(RequestStatus.Pending, requisition.Status);
        Assert.All(requisition.Items, item => Assert.Equal(RequestStatus.Pending, item.Status));
        Assert.Empty(context.RequisitionApprovals);
        Assert.Single(context.ApprovalActionLogs);
    }

    [Theory]
    [InlineData("RawStockRequisition")]
    [InlineData("PackageStockRequisition")]
    public async Task CreateInitialApprovalsAsync_UsesSharedStockWorkflow(
        string creationModelType)
    {
        await using var context = CreateContext();
        var requisition = CreateStockRequisition();
        var approval = new Approval
        {
            Id = Guid.NewGuid(),
            ItemType = "StockRequisition",
            ApprovalStages = [],
        };
        approval.ApprovalStages.Add(new ApprovalStage
        {
            Id = Guid.NewGuid(),
            ApprovalId = approval.Id,
            Approval = approval,
            Order = 1,
            Required = true,
            UserId = Guid.NewGuid(),
        });
        context.AddRange(requisition, approval);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(
            creationModelType,
            requisition.Id
        );

        Assert.False(requisition.Approved);
        var pending = Assert.Single(context.RequisitionApprovals);
        Assert.Equal(approval.Id, pending.ApprovalId);
        Assert.Equal(ApprovalStatus.Pending, pending.Status);
        Assert.Empty(context.ApprovalActionLogs);
    }

    [Theory]
    [InlineData("RawStockRequisition")]
    [InlineData("PackageStockRequisition")]
    public async Task CreateInitialApprovalsAsync_AutoApprovesBothStockFlows_WhenSharedWorkflowIsMissing(
        string creationModelType)
    {
        await using var context = CreateContext();
        var requisition = CreateStockRequisition();
        context.Requisitions.Add(requisition);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(
            creationModelType,
            requisition.Id
        );

        Assert.True(requisition.Approved);
        Assert.Equal(RequestStatus.Pending, requisition.Status);
        Assert.All(requisition.Items, item => Assert.Equal(RequestStatus.Pending, item.Status));
        Assert.Empty(context.RequisitionApprovals);
        Assert.Single(context.ApprovalActionLogs);
    }

    private static Requisition CreatePurchaseRequisition() =>
        new()
        {
            Id = Guid.NewGuid(),
            Code = "PR-AUTO-001",
            RequestedById = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            RequisitionType = RequisitionType.Purchase,
            Status = RequestStatus.New,
            Approved = false,
            Items =
            [
                new RequisitionItem
                {
                    Id = Guid.NewGuid(),
                    MaterialId = Guid.NewGuid(),
                    Status = RequestStatus.New,
                    Quantity = 1,
                },
            ],
        };

    private static Requisition CreateStockRequisition() =>
        new()
        {
            Id = Guid.NewGuid(),
            Code = "SR-AUTO-001",
            RequestedById = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            RequisitionType = RequisitionType.Stock,
            Status = RequestStatus.New,
            Approved = false,
            Items =
            [
                new RequisitionItem
                {
                    Id = Guid.NewGuid(),
                    MaterialId = Guid.NewGuid(),
                    Status = RequestStatus.New,
                    Quantity = 1,
                },
            ],
        };

    private static ApprovalRepository CreateRepository(ApplicationDbContext context) =>
        new(
            context,
            null!,
            null!,
            null!,
            NullLogger<ApprovalRepository>.Instance,
            null!,
            new NoOpProductionActivityStepEventPublisher()
        );

    private static ApplicationDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new RequisitionAutoApprovalCurrentUser()
        );
}
