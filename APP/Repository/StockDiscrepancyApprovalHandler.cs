using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.StockAdjustments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class StockDiscrepancyApprovalHandler
{
    internal static async Task CreateAsync(ApplicationDbContext context, Guid id,
        IReadOnlyCollection<ApprovalStage> stages, Approval approval)
    {
        if (stages.Count == 0 || !stages.Any(stage => stage.Required))
            throw new InvalidOperationException("Stock discrepancy requires assigned approval stages.");
        if (await context.StockDiscrepancyApprovals.AnyAsync(stage =>
            stage.StockDiscrepancyId == id)) return;
        var first = stages.Min(stage => stage.Order);
        var now = DateTime.UtcNow;
        context.StockDiscrepancyApprovals.AddRange(stages.Select(stage =>
            new StockDiscrepancyApproval
            {
                StockDiscrepancyId = id,
                ApprovalId = approval.Id,
                Required = stage.Required,
                Order = stage.Order,
                UserId = stage.UserId,
                RoleId = stage.RoleId,
                CreatedAt = now,
                ActivatedAt = stage.Order == first ? now : null,
            }));
        await context.SaveChangesAsync();
    }

    internal static async Task<Result> ReviewAsync(ApplicationDbContext context, Guid id,
        Guid reviewerId, IReadOnlyCollection<Guid> roles, string comments, ApprovalStatus decision)
    {
        var report = await context.StockDiscrepancies.Include(item => item.Approvals)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (report is null) return Error.NotFound("StockDiscrepancy.NotFound", "Report not found.");
        if (report.Approved || report.Rejected)
            return Error.Validation("StockDiscrepancy.Final", "Report already has a final decision.");
        if (report.CreatedById == reviewerId)
            return Error.Validation("Approval.SelfReview", "Reporter cannot review their own discrepancy.");
        var stage = report.Approvals
            .Where(item => item.Status == ApprovalStatus.Pending && item.ActivatedAt.HasValue)
            .Where(item => item.UserId == reviewerId ||
                (item.RoleId.HasValue && roles.Contains(item.RoleId.Value)))
            .OrderBy(item => item.Order).FirstOrDefault();
        if (stage is null)
            return Error.Validation("Approval.Unauthorized", "No active approval stage is assigned to you.");
        stage.Status = decision;
        stage.ApprovalTime = DateTime.UtcNow;
        stage.ApprovedById = reviewerId;
        stage.Comments = comments;
        if (decision == ApprovalStatus.Rejected) report.Rejected = true;
        else
        {
            if (report.Approvals.Where(item => item.Required && item.Order == stage.Order)
                .All(item => item.Status == ApprovalStatus.Approved))
            {
                var nextOrder = report.Approvals
                    .Where(item => item.Required && item.Order > stage.Order &&
                        item.Status == ApprovalStatus.Pending)
                    .Select(item => (int?)item.Order).Min();
                if (nextOrder.HasValue)
                    foreach (var next in report.Approvals.Where(item =>
                        item.Order == nextOrder.Value && !item.ActivatedAt.HasValue))
                        next.ActivatedAt = DateTime.UtcNow;
            }
            report.Approved = report.Approvals.Where(item => item.Required)
                .All(item => item.Status == ApprovalStatus.Approved);
        }
        context.ApprovalActionLogs.Add(new ApprovalActionLog
        {
            ModelId = id,
            UserId = reviewerId,
            Status = decision,
            Comments = comments,
        });
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
