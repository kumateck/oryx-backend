using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.ProductionOrders;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class AllocateProductionOrderApprovalHandler
{
    internal static async Task CreateAsync(
        ApplicationDbContext context,
        Guid allocationId,
        IReadOnlyCollection<ApprovalStage> stages,
        Approval approval)
    {
        if (await context.AllocateProductionOrderApprovals.AnyAsync(item =>
                item.AllocateProductionOrderId == allocationId
                && item.ApprovalId == approval.Id))
            return;

        await context.AllocateProductionOrderApprovals.AddRangeAsync(stages.Select(stage =>
            new AllocateProductionOrderApprovals
            {
                AllocateProductionOrderId = allocationId,
                ApprovalId = approval.Id,
                Required = stage.Required,
                Order = stage.Order,
                UserId = stage.UserId,
                RoleId = stage.RoleId,
                ActivatedAt = stage.Order == 1 ? DateTime.UtcNow : null,
            }));
        await context.SaveChangesAsync();
    }

    internal static async Task<Result> ApproveAsync(
        ApplicationDbContext context,
        Guid allocationId,
        Guid userId,
        IReadOnlyCollection<Guid> roleIds,
        string comments)
    {
        var allocation = await context.AllocateProductionOrders
            .Include(item => item.Approvals)
            .SingleOrDefaultAsync(item => item.Id == allocationId);
        if (allocation is null)
            return Error.NotFound(
                "AllocateProductionOrder.NotFound",
                $"Allocation {allocationId} was not found.");

        var stage = GetAssignedStage(allocation.Approvals, userId, roleIds);
        if (stage is null)
            return Unauthorized("approve");

        stage.Status = ApprovalStatus.Approved;
        stage.ApprovalTime = DateTime.UtcNow;
        stage.ApprovedById = userId;
        stage.Comments = comments;

        if (allocation.Approvals.Where(item => item.Required)
            .All(item => item.Status == ApprovalStatus.Approved))
            allocation.Approved = true;

        ActivateNextOrder(allocation.Approvals);
        await AddLogAndSave(context, allocationId, userId, ApprovalStatus.Approved, comments);
        return Result.Success();
    }

    internal static async Task<Result> RejectAsync(
        ApplicationDbContext context,
        Guid allocationId,
        Guid userId,
        IReadOnlyCollection<Guid> roleIds,
        string comments)
    {
        var allocation = await context.AllocateProductionOrders
            .Include(item => item.Approvals)
            .SingleOrDefaultAsync(item => item.Id == allocationId);
        if (allocation is null)
            return Error.NotFound(
                "AllocateProductionOrder.NotFound",
                $"Allocation {allocationId} was not found.");

        var stage = GetAssignedStage(allocation.Approvals, userId, roleIds);
        if (stage is null)
            return Unauthorized("reject");

        stage.Status = ApprovalStatus.Rejected;
        stage.ApprovalTime = DateTime.UtcNow;
        stage.ApprovedById = userId;
        stage.Comments = comments;
        await AddLogAndSave(context, allocationId, userId, ApprovalStatus.Rejected, comments);
        return Result.Success();
    }

    private static AllocateProductionOrderApprovals GetAssignedStage(
        IEnumerable<AllocateProductionOrderApprovals> stages,
        Guid userId,
        IReadOnlyCollection<Guid> roleIds)
    {
        return stages
            .Where(item => item.Status == ApprovalStatus.Pending)
            .OrderByDescending(item => item.Required)
            .ThenBy(item => item.Order)
            .FirstOrDefault(item => item.UserId == userId
                || item.RoleId.HasValue && roleIds.Contains(item.RoleId.Value));
    }

    private static void ActivateNextOrder(IEnumerable<AllocateProductionOrderApprovals> stages)
    {
        var nextOrder = stages
            .Where(item => item.Status == ApprovalStatus.Pending)
            .Select(item => (int?)item.Order)
            .Min();
        if (!nextOrder.HasValue)
            return;

        foreach (var stage in stages.Where(item =>
                     item.Status == ApprovalStatus.Pending && item.Order == nextOrder))
            stage.ActivatedAt ??= DateTime.UtcNow;
    }

    private static async Task AddLogAndSave(
        ApplicationDbContext context,
        Guid modelId,
        Guid userId,
        ApprovalStatus status,
        string comments)
    {
        await context.ApprovalActionLogs.AddAsync(new ApprovalActionLog
        {
            ModelId = modelId,
            UserId = userId,
            Status = status,
            Comments = comments,
        });
        await context.SaveChangesAsync();
    }

    private static Error Unauthorized(string action) => Error.Validation(
        "Approval.Unauthorized",
        $"You are not authorized to {action} this resource at this time.");
}
