using APP.Repository;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class RecheckCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

/// <summary>
/// A revised (or rejected) purchase order that is checked again must go back to its approvers.
/// Before the fix, the earlier round's rows made the check skip creating approvals, so the order
/// sat at Checked with nothing in anyone's pending list.
/// </summary>
public class PurchaseOrderRecheckApprovalTests
{
    [Fact]
    public async Task CheckPurchaseOrder_StartsNewRound_WhenRevisedOrderWasApprovedBefore()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var (order, approval) = await SeedAsync(context, approverId, PurchaseOrderStatus.Revised);
        context.PurchaseOrderApprovals.Add(DecidedStage(order.Id, approval.Id, approverId, ApprovalStatus.Approved));
        order.Approved = true;
        await context.SaveChangesAsync();

        var result = await CreateProcurement(context).CheckPurchaseOrder(order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(PurchaseOrderStatus.Checked, order.Status);
        Assert.False(order.Approved);
        var pending = Assert.Single(context.PurchaseOrderApprovals);
        Assert.Equal(ApprovalStatus.Pending, pending.Status);
        Assert.Equal(approverId, pending.UserId);
        Assert.NotNull(pending.ActivatedAt);
    }

    [Fact]
    public async Task CheckPurchaseOrder_StartsNewRound_WhenEarlierRoundWasRejected()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var (order, approval) = await SeedAsync(context, approverId, PurchaseOrderStatus.Revised);
        context.PurchaseOrderApprovals.Add(DecidedStage(order.Id, approval.Id, approverId, ApprovalStatus.Rejected));
        await context.SaveChangesAsync();

        await CreateProcurement(context).CheckPurchaseOrder(order.Id);

        var pending = Assert.Single(context.PurchaseOrderApprovals);
        Assert.Equal(ApprovalStatus.Pending, pending.Status);
        Assert.NotNull(pending.ActivatedAt);
    }

    [Fact]
    public async Task NewRound_CanBeApproved_AndKeepsEarlierDecisionsInHistory()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var (order, approval) = await SeedAsync(context, approverId, PurchaseOrderStatus.Revised);
        context.PurchaseOrderApprovals.Add(DecidedStage(order.Id, approval.Id, approverId, ApprovalStatus.Rejected));
        context.ApprovalActionLogs.Add(new ApprovalActionLog
        {
            ModelId = order.Id,
            UserId = approverId,
            Status = ApprovalStatus.Rejected,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
        });
        await context.SaveChangesAsync();
        var approvals = CreateApprovals(context);

        await CreateProcurement(context, approvals).CheckPurchaseOrder(order.Id);
        var result = await approvals.ApproveItem(nameof(PurchaseOrder), order.Id, approverId, []);

        Assert.True(result.IsSuccess);
        Assert.True(order.Approved);
        Assert.Equal(PurchaseOrderStatus.Approved, order.Status);
        Assert.Equal(
            [ApprovalStatus.Rejected, ApprovalStatus.Approved],
            context.ApprovalActionLogs.Where(log => log.ModelId == order.Id)
                .OrderBy(log => log.CreatedAt).Select(log => log.Status).ToList());
    }

    [Fact]
    public async Task CheckPurchaseOrder_Twice_KeepsTheWaitingRound()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var (order, _) = await SeedAsync(context, approverId, PurchaseOrderStatus.PendingCheck);
        var procurement = CreateProcurement(context);

        await procurement.CheckPurchaseOrder(order.Id);
        var first = Assert.Single(context.PurchaseOrderApprovals);
        await procurement.CheckPurchaseOrder(order.Id);

        Assert.Equal(first.Id, Assert.Single(context.PurchaseOrderApprovals).Id);
    }

    private static async Task<(PurchaseOrder Order, Approval Approval)> SeedAsync(
        ApplicationDbContext context, Guid approverId, PurchaseOrderStatus status)
    {
        // PurchaseOrder.Supplier is auto-included and required, so the InMemory
        // provider hides the order from queries unless the supplier exists.
        var supplier = new Supplier { Id = Guid.NewGuid() };
        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            Code = "EPRC-TEST",
            SourceRequisitionId = Guid.NewGuid(),
            SupplierId = supplier.Id,
            Status = status,
        };
        var approval = new Approval { Id = Guid.NewGuid(), ItemType = nameof(PurchaseOrder), ApprovalStages = [] };
        approval.ApprovalStages.Add(new ApprovalStage
        {
            Id = Guid.NewGuid(),
            ApprovalId = approval.Id,
            Approval = approval,
            Order = 1,
            Required = true,
            UserId = approverId,
        });
        context.AddRange(supplier, order, approval);
        await context.SaveChangesAsync();
        return (order, approval);
    }

    private static PurchaseOrderApproval DecidedStage(
        Guid orderId, Guid approvalId, Guid approverId, ApprovalStatus status) =>
        new()
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = orderId,
            ApprovalId = approvalId,
            Order = 1,
            Required = true,
            UserId = approverId,
            Status = status,
            ActivatedAt = DateTime.UtcNow.AddDays(-3),
            ApprovalTime = DateTime.UtcNow.AddDays(-2),
        };

    private static ApprovalRepository CreateApprovals(ApplicationDbContext context) =>
        new(context, null!, null!, null!, NullLogger<ApprovalRepository>.Instance, null!,
            new NoOpProductionActivityStepEventPublisher());

    private static ProcurementRepository CreateProcurement(
        ApplicationDbContext context, ApprovalRepository approvals = null) =>
        new(context, null!, null!, null!, approvals ?? CreateApprovals(context), null!, null!);

    private static ApplicationDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new RecheckCurrentUser()
        );
}
