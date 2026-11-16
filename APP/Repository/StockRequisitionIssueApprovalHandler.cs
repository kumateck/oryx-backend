using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>
/// The warehouse check of an issued stock requisition, as approval stages: it now happens on the issue's
/// document review. Stages remain for issues checked before that, whose voucher still names their checker.
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

    /// <summary>The person whose approval completed the check: the last required approver.</summary>
    internal static StockRequisitionIssueApproval Checker(StockRequisitionIssue issue) =>
        !issue.Approved ? null : issue.Approvals
            .Where(stage => stage.Status == ApprovalStatus.Approved && stage.ApprovedById.HasValue)
            .OrderByDescending(stage => stage.Required).ThenByDescending(stage => stage.Order)
            .ThenByDescending(stage => stage.ApprovalTime).FirstOrDefault();
}
