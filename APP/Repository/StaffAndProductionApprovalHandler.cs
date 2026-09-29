using System.Data;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.StaffRequisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class StaffAndProductionApprovalHandler
{
    internal static Task<Result> ReviewStaffAsync(
        ApplicationDbContext context, Guid id, Guid userId, List<Guid> roleIds,
        string comments, ApprovalStatus decision) =>
        ReviewAsync(context, nameof(StaffRequisition), id, userId, roleIds, comments, decision);

    internal static Task<Result> ReviewProductionAsync(
        ApplicationDbContext context, Guid id, Guid userId, List<Guid> roleIds,
        string comments, ApprovalStatus decision) =>
        ReviewAsync(context, "ProductionOrder", id, userId, roleIds, comments, decision);

    private static async Task<Result> ReviewAsync(
        ApplicationDbContext context, string modelType, Guid id, Guid userId,
        List<Guid> roleIds, string comments, ApprovalStatus decision)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

        List<ResponsibleApprovalStage> stages;
        Guid? creatorId;
        Action<bool> setFinalState;
        if (modelType == nameof(StaffRequisition))
        {
            var entity = await context.StaffRequisitions.Include(item => item.Approvals)
                .FirstOrDefaultAsync(item => item.Id == id);
            if (entity is null)
                return Error.NotFound("StaffRequisition.NotFound", "Staff requisition not found.");
            creatorId = entity.CreatedById;
            stages = entity.Approvals.Cast<ResponsibleApprovalStage>().ToList();
            setFinalState = approved =>
            {
                entity.Approved = approved;
                entity.StaffRequisitionStatus = approved
                    ? StaffRequisitionStatus.Approved : StaffRequisitionStatus.Rejected;
            };
        }
        else
        {
            var entity = await context.ProductionOrders.Include(item => item.Approvals)
                .FirstOrDefaultAsync(item => item.Id == id);
            if (entity is null)
                return Error.NotFound("ProductionOrder.NotFound", "Production order not found.");
            creatorId = entity.CreatedById;
            stages = entity.Approvals.Cast<ResponsibleApprovalStage>().ToList();
            setFinalState = approved => entity.Approved = approved;
        }

        if (creatorId == userId)
            return Error.Validation("Approval.MakerChecker", "The creator cannot review this request.");

        var stage = stages.Where(item => item.Status == ApprovalStatus.Pending
                    && item.ActivatedAt.HasValue)
            .OrderBy(item => item.Order).FirstOrDefault();
        if (stage is null)
            return Error.Conflict("Approval.NoActiveStage", "No approval stage is active.");
        if (stage.UserId != userId
            && !(stage.RoleId.HasValue && roleIds?.Contains(stage.RoleId.Value) == true))
            return Error.Validation("Approval.Unauthorized", "You are not assigned to this stage.");

        stage.Status = decision;
        stage.Comments = comments?.Trim();
        stage.ApprovalTime = DateTime.UtcNow;
        stage.ApprovedById = userId;
        context.ApprovalActionLogs.Add(new ApprovalActionLog
        {
            ModelId = id, UserId = userId, Status = decision, Comments = comments?.Trim(),
        });

        if (decision == ApprovalStatus.Rejected)
            setFinalState(false);
        else
        {
            var next = stages.Where(item => item.Status == ApprovalStatus.Pending
                        && item.Order > stage.Order)
                .OrderBy(item => item.Order).FirstOrDefault();
            if (next is null)
                setFinalState(true);
            else
                next.ActivatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        if (transaction is not null)
            await transaction.CommitAsync();
        return Result.Success();
    }
}
