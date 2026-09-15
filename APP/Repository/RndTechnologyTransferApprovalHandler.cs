using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.RndTechnologyTransfers;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class RndTechnologyTransferApprovalHandler
{
    internal static async Task CreateAsync(ApplicationDbContext context, Guid id,
        IReadOnlyCollection<ApprovalStage> stages, Approval approval)
    {
        var existing = await context.RndTechnologyTransferApprovals
            .Where(item => item.RndTechnologyTransferId == id && item.ApprovalId == approval.Id).ToListAsync();
        if (existing.Count != 0)
        {
            foreach (var item in existing)
            {
                item.Status = ApprovalStatus.Pending; item.ApprovalTime = null; item.ApprovedById = null;
                item.Comments = null; item.ActivatedAt = item.Order == stages.Min(stage => stage.Order) ? DateTime.UtcNow : null;
            }
        }
        else
            await context.RndTechnologyTransferApprovals.AddRangeAsync(stages.Select(stage =>
                new RndTechnologyTransferApprovals
                {
                    RndTechnologyTransferId = id, ApprovalId = approval.Id, Required = stage.Required,
                    Order = stage.Order, UserId = stage.UserId, RoleId = stage.RoleId,
                    ActivatedAt = stage.Order == stages.Min(item => item.Order) ? DateTime.UtcNow : null,
                }));
        await context.SaveChangesAsync();
    }

    internal static Task<Result> ApproveAsync(ApplicationDbContext context, Guid id, Guid userId,
        IReadOnlyCollection<Guid> roleIds, string comments) =>
        ReviewAsync(context, id, userId, roleIds, comments, ApprovalStatus.Approved);

    internal static Task<Result> RejectAsync(ApplicationDbContext context, Guid id, Guid userId,
        IReadOnlyCollection<Guid> roleIds, string comments) =>
        ReviewAsync(context, id, userId, roleIds, comments, ApprovalStatus.Rejected);

    private static async Task<Result> ReviewAsync(ApplicationDbContext context, Guid id, Guid userId,
        IReadOnlyCollection<Guid> roleIds, string comments, ApprovalStatus decision)
    {
        var transfer = await context.RndTechnologyTransfers.Include(item => item.Approvals)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (transfer is null) return Error.NotFound("RndTechnologyTransfer.NotFound", "Technology transfer not found.");
        if (transfer.Status != RndTechnologyTransferStatus.ProtocolInReview)
            return Error.Conflict("RndTechnologyTransfer.Status", "Only a submitted protocol can be decided.");
        if (transfer.CreatedById == userId)
            return Error.Validation("RndTechnologyTransfer.MakerChecker", "The transfer creator cannot review its protocol.");
        var stage = transfer.Approvals.Where(item => item.Status == ApprovalStatus.Pending && item.ActivatedAt.HasValue)
            .OrderBy(item => item.Order).FirstOrDefault(item => item.UserId == userId
                || item.RoleId.HasValue && roleIds.Contains(item.RoleId.Value));
        if (stage is null) return Error.Validation("Approval.Unauthorized", "No assigned protocol stage is active.");
        stage.Status = decision; stage.ApprovalTime = DateTime.UtcNow; stage.ApprovedById = userId;
        stage.Comments = comments?.Trim();
        context.ApprovalActionLogs.Add(new ApprovalActionLog
            { ModelId = id, UserId = userId, Status = decision, Comments = comments?.Trim() });
        if (decision == ApprovalStatus.Rejected)
        {
            transfer.Status = RndTechnologyTransferStatus.GapAnalysis; transfer.Approved = false;
        }
        else
        {
            var next = transfer.Approvals.Where(item => item.Status == ApprovalStatus.Pending && item.Order > stage.Order)
                .OrderBy(item => item.Order).FirstOrDefault();
            if (next is null)
            {
                transfer.Status = RndTechnologyTransferStatus.ProtocolApproved; transfer.Approved = true;
                transfer.ProtocolApprovedAt = DateTime.UtcNow; transfer.ProtocolApprovedById = userId;
                transfer.ProtocolApprovalComments = comments?.Trim();
            }
            else next.ActivatedAt = DateTime.UtcNow;
        }
        transfer.LastUpdatedById = userId;
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
