using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>
/// The warehouse check of an issued stock requisition. Its approver signs the voucher as "Checked by".
/// </summary>
internal static class StockRequisitionIssueApprovalHandler
{
    internal const string ModelType = nameof(StockRequisitionIssue);

    internal static async Task CreateAsync(ApplicationDbContext context, Guid id,
        IReadOnlyCollection<ApprovalStage> stages, Approval approval)
    {
        if (await context.StockRequisitionIssueApprovals.AnyAsync(stage => stage.StockRequisitionIssueId == id)) return;
        var first = stages.Min(stage => stage.Order);
        var now = DateTime.UtcNow;
        context.StockRequisitionIssueApprovals.AddRange(stages.Select(stage =>
            new StockRequisitionIssueApproval
            {
                StockRequisitionIssueId = id,
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
        var issue = await context.StockRequisitionIssues.Include(item => item.Approvals)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (issue is null)
            return Error.NotFound("StockRequisitionIssue.NotFound", "Issued stock requisition not found.");
        if (issue.Approved || issue.Rejected)
            return Error.Validation("StockRequisitionIssue.Final", "This issue has already been checked.");
        if (issue.IssuedById == reviewerId)
            return Error.Validation("Approval.SelfReview", "The person who issued the stock cannot check their own issue.");
        var stage = issue.Approvals
            .Where(item => item.Status == ApprovalStatus.Pending && item.ActivatedAt.HasValue)
            .Where(item => item.UserId == reviewerId ||
                (item.RoleId.HasValue && roles.Contains(item.RoleId.Value)))
            .OrderBy(item => item.Order).FirstOrDefault();
        if (stage is null)
            return Error.Validation("Approval.Unauthorized", "No active approval stage is assigned to you.");
        var now = DateTime.UtcNow;
        stage.Status = decision;
        stage.ApprovalTime = now;
        stage.ApprovedById = reviewerId;
        stage.Comments = comments;
        if (decision == ApprovalStatus.Rejected) issue.Rejected = true;
        else
        {
            if (issue.Approvals.Where(item => item.Required && item.Order == stage.Order)
                .All(item => item.Status == ApprovalStatus.Approved))
            {
                var nextOrder = issue.Approvals
                    .Where(item => item.Required && item.Order > stage.Order && item.Status == ApprovalStatus.Pending)
                    .Select(item => (int?)item.Order).Min();
                if (nextOrder.HasValue)
                    foreach (var next in issue.Approvals.Where(item => item.Order == nextOrder.Value && !item.ActivatedAt.HasValue))
                        next.ActivatedAt = now;
            }
            issue.Approved = issue.Approvals.Where(item => item.Required)
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

    /// <summary>The person whose approval completed the check: the last required approver.</summary>
    internal static StockRequisitionIssueApproval Checker(StockRequisitionIssue issue) =>
        !issue.Approved ? null : issue.Approvals
            .Where(stage => stage.Status == ApprovalStatus.Approved && stage.ApprovedById.HasValue)
            .OrderByDescending(stage => stage.Required).ThenByDescending(stage => stage.Order)
            .ThenByDescending(stage => stage.ApprovalTime).FirstOrDefault();
}
