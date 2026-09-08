using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.RndProjects;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class RndProjectApprovalHandler
{
    internal static async Task CreateAsync(
        ApplicationDbContext context,
        Guid rndProjectId,
        IReadOnlyCollection<ApprovalStage> stages,
        Approval approval)
    {
        if (await context.RndProjectApprovals.AnyAsync(item =>
                item.RndProjectId == rndProjectId
                && item.ApprovalId == approval.Id))
            return;

        await context.RndProjectApprovals.AddRangeAsync(stages.Select(stage =>
            new RndProjectApprovals
            {
                RndProjectId = rndProjectId,
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
        Guid rndProjectId,
        Guid userId,
        IReadOnlyCollection<Guid> roleIds,
        string comments)
    {
        var rndProject = await context.RndProjects
            .Include(item => item.Approvals)
            .SingleOrDefaultAsync(item => item.Id == rndProjectId);
        if (rndProject is null)
            return Error.NotFound(
                "RndProject.NotFound",
                $"R&D project {rndProjectId} was not found.");

        var stage = GetAssignedStage(rndProject.Approvals, userId, roleIds);
        if (stage is null)
            return Unauthorized("approve");

        stage.Status = ApprovalStatus.Approved;
        stage.ApprovalTime = DateTime.UtcNow;
        stage.ApprovedById = userId;
        stage.Comments = comments;

        if (rndProject.Approvals.Where(item => item.Required)
            .All(item => item.Status == ApprovalStatus.Approved))
        {
            rndProject.Approved = true;
            rndProject.Status = RndProjectStatus.InDevelopment;
        }

        ActivateNextOrder(rndProject.Approvals);
        await AddLogAndSave(context, rndProjectId, userId, ApprovalStatus.Approved, comments);
        return Result.Success();
    }

    internal static async Task<Result> RejectAsync(
        ApplicationDbContext context,
        Guid rndProjectId,
        Guid userId,
        IReadOnlyCollection<Guid> roleIds,
        string comments)
    {
        var rndProject = await context.RndProjects
            .Include(item => item.Approvals)
            .SingleOrDefaultAsync(item => item.Id == rndProjectId);
        if (rndProject is null)
            return Error.NotFound(
                "RndProject.NotFound",
                $"R&D project {rndProjectId} was not found.");

        var stage = GetAssignedStage(rndProject.Approvals, userId, roleIds);
        if (stage is null)
            return Unauthorized("reject");

        stage.Status = ApprovalStatus.Rejected;
        stage.ApprovalTime = DateTime.UtcNow;
        stage.ApprovedById = userId;
        stage.Comments = comments;
        await AddLogAndSave(context, rndProjectId, userId, ApprovalStatus.Rejected, comments);
        return Result.Success();
    }

    private static RndProjectApprovals GetAssignedStage(
        IEnumerable<RndProjectApprovals> stages,
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

    private static void ActivateNextOrder(IEnumerable<RndProjectApprovals> stages)
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
