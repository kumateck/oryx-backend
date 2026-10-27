using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Shipments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class ShipmentDiscrepancyApprovalHandler
{
    internal static async Task CreateAsync(ApplicationDbContext context, Guid caseId,
        IReadOnlyCollection<ApprovalStage> stages, Approval approval)
    {
        if (stages.Count == 0 || !stages.Any(stage => stage.Required))
            throw new InvalidOperationException("Shipment discrepancy requires assigned approval stages.");
        if (await context.ShipmentDiscrepancyApprovals.AnyAsync(stage =>
            stage.ShipmentDiscrepancyId == caseId)) return;

        var firstOrder = stages.Min(stage => stage.Order);
        var now = DateTime.UtcNow;
        await context.ShipmentDiscrepancyApprovals.AddRangeAsync(stages.Select(stage =>
            new ShipmentDiscrepancyApproval
            {
                ShipmentDiscrepancyId = caseId,
                ApprovalId = approval.Id,
                Required = stage.Required,
                Order = stage.Order,
                UserId = stage.UserId,
                RoleId = stage.RoleId,
                CreatedAt = now,
                ActivatedAt = stage.Order == firstOrder ? now : null,
            }));
        await context.SaveChangesAsync();
    }

    internal static async Task<Result> ReviewAsync(ApplicationDbContext context,
        Guid caseId, Guid reviewerId, IReadOnlyCollection<Guid> roleIds,
        string comments, ApprovalStatus decision)
    {
        var discrepancy = await context.ShipmentDiscrepancies
            .Include(item => item.Approvals)
            .SingleOrDefaultAsync(item => item.Id == caseId);
        if (discrepancy is null)
            return Error.NotFound("ShipmentDiscrepancy.NotFound", "Shipment discrepancy not found.");
        if (discrepancy.Approved || discrepancy.Rejected)
            return Error.Validation("ShipmentDiscrepancy.Final", "This discrepancy has a final decision.");
        if (discrepancy.CreatedById == reviewerId)
            return Error.Validation("Approval.SelfReview", "The reporter cannot approve their own discrepancy.");

        var stage = discrepancy.Approvals
            .Where(item => item.Status == ApprovalStatus.Pending && item.ActivatedAt.HasValue)
            .Where(item => item.UserId == reviewerId ||
                (item.RoleId.HasValue && roleIds.Contains(item.RoleId.Value)))
            .OrderBy(item => item.Order)
            .FirstOrDefault();
        if (stage is null)
            return Error.Validation("Approval.Unauthorized", "No active approval stage is assigned to you.");

        stage.Status = decision;
        stage.ApprovalTime = DateTime.UtcNow;
        stage.ApprovedById = reviewerId;
        stage.Comments = comments;
        if (decision == ApprovalStatus.Rejected)
            discrepancy.Rejected = true;
        else
        {
            var currentOrderComplete = discrepancy.Approvals
                .Where(item => item.Order == stage.Order && item.Required)
                .All(item => item.Status == ApprovalStatus.Approved);
            if (currentOrderComplete)
            {
                var nextOrder = discrepancy.Approvals
                    .Where(item => item.Order > stage.Order && item.Required &&
                        item.Status == ApprovalStatus.Pending)
                    .Select(item => (int?)item.Order).Min();
                if (nextOrder.HasValue)
                {
                    foreach (var next in discrepancy.Approvals.Where(item =>
                        item.Order == nextOrder.Value && !item.ActivatedAt.HasValue))
                        next.ActivatedAt = DateTime.UtcNow;
                }
            }
            discrepancy.Approved = discrepancy.Approvals.Where(item => item.Required)
                .All(item => item.Status == ApprovalStatus.Approved);
        }

        context.ApprovalActionLogs.Add(new ApprovalActionLog
        {
            ModelId = caseId,
            UserId = reviewerId,
            Status = decision,
            Comments = comments,
        });
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
