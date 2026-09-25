using System.Data;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Procurement.Suppliers;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class SupplierPricingAgreementApprovalHandler
{
    internal static async Task CreateAsync(ApplicationDbContext context, Guid proposalId,
        List<ApprovalStage> stages, Approval approval)
    {
        if (await context.SupplierPricingAgreementApprovals.AnyAsync(item =>
                item.SupplierPricingAgreementId == proposalId && item.ApprovalId == approval.Id)) return;
        await context.SupplierPricingAgreementApprovals.AddRangeAsync(stages.Select((stage, index) =>
            new SupplierPricingAgreementApproval {
                SupplierPricingAgreementId = proposalId, ApprovalId = approval.Id,
                Order = stage.Order, Required = stage.Required, UserId = stage.UserId,
                RoleId = stage.RoleId, ActivatedAt = index == 0 ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow,
            }));
        await context.SaveChangesAsync();
    }

    internal static Task<Result> ApproveAsync(ApplicationDbContext context, Guid id, Guid userId,
        List<Guid> roleIds, string comments) => ReviewAsync(context, id, userId, roleIds,
        comments, ApprovalStatus.Approved);

    internal static Task<Result> RejectAsync(ApplicationDbContext context, Guid id, Guid userId,
        List<Guid> roleIds, string comments) => ReviewAsync(context, id, userId, roleIds,
        comments, ApprovalStatus.Rejected);

    private static async Task<Result> ReviewAsync(ApplicationDbContext context, Guid id,
        Guid userId, List<Guid> roleIds, string comments, ApprovalStatus decision)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var proposal = await context.SupplierPricingAgreements.Include(item => item.Approvals)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (proposal is null)
            return Error.NotFound("SupplierPricingAgreement.NotFound", "Agreement proposal not found.");
        if (proposal.Status != SupplierPricingAgreementStatus.Pending)
            return Error.Conflict("SupplierPricingAgreement.Status", "Only a pending agreement can be reviewed.");
        if (proposal.CreatedById == userId)
            return Error.Validation("SupplierPricingAgreement.MakerChecker",
                "The proposer cannot approve or reject this agreement.");
        var stage = proposal.Approvals.Where(item => item.Status == ApprovalStatus.Pending
                && item.ActivatedAt.HasValue).OrderBy(item => item.Order).FirstOrDefault();
        if (stage is null)
            return Error.Conflict("SupplierPricingAgreement.Stage", "No approval stage is active.");
        if (stage.UserId != userId && (!stage.RoleId.HasValue
            || roleIds?.Contains(stage.RoleId.Value) != true))
            return Error.Validation("Approval.Unauthorized", "You are not assigned to this approval stage.");

        var next = decision == ApprovalStatus.Approved
            ? proposal.Approvals.Where(item => item.Status == ApprovalStatus.Pending
                && item.Order > stage.Order).OrderBy(item => item.Order).FirstOrDefault()
            : null;
        if (decision == ApprovalStatus.Approved && next is null)
        {
            var activation = await ActivateAsync(context, proposal, userId);
            if (!activation.IsSuccess) return activation;
        }
        if (decision == ApprovalStatus.Rejected)
            proposal.Status = SupplierPricingAgreementStatus.Rejected;
        stage.Status = decision;
        stage.Comments = comments?.Trim();
        stage.ApprovalTime = DateTime.UtcNow;
        stage.ApprovedById = userId;
        if (next is not null) next.ActivatedAt = DateTime.UtcNow;
        context.ApprovalActionLogs.Add(new ApprovalActionLog {
            ModelId = proposal.Id, UserId = userId, Status = decision,
            Comments = comments?.Trim(),
        });
        proposal.UpdatedAt = DateTime.UtcNow;
        proposal.LastUpdatedById = userId;
        await context.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return Result.Success();
    }

    internal static async Task<Result> ActivateAsync(ApplicationDbContext context,
        SupplierPricingAgreement proposal, Guid? userId)
    {
        if (proposal.Status != SupplierPricingAgreementStatus.Pending)
            return Error.Conflict("SupplierPricingAgreement.Status", "Agreement proposal is not pending.");
        if (proposal.ChangeKind != SupplierPricingAgreementChangeKind.Archive
            && !await context.SupplierManufacturers.AnyAsync(item =>
                item.SupplierId == proposal.SupplierId && item.MaterialId == proposal.MaterialId
                && item.UoMId == proposal.UoMId))
            return Error.Conflict("SupplierPricingAgreement.Association",
                "Material and unit are no longer associated with this supplier.");
        SupplierPricingAgreement target = null;
        if (proposal.ReplacesAgreementId.HasValue)
        {
            target = await context.SupplierPricingAgreements.FirstOrDefaultAsync(item =>
                item.Id == proposal.ReplacesAgreementId.Value && item.SupplierId == proposal.SupplierId
                && item.Status == SupplierPricingAgreementStatus.Approved);
            if (target is null || target.MaterialId != proposal.MaterialId || target.UoMId != proposal.UoMId)
                return Error.Conflict("SupplierPricingAgreement.Target", "The agreement being changed is no longer active.");
        }
        if (proposal.ChangeKind != SupplierPricingAgreementChangeKind.Archive)
        {
            var overlap = await context.SupplierPricingAgreements.AnyAsync(item =>
                item.Id != proposal.Id && item.Id != proposal.ReplacesAgreementId
                && item.Status == SupplierPricingAgreementStatus.Approved
                && item.ChangeKind != SupplierPricingAgreementChangeKind.Archive
                && item.SupplierId == proposal.SupplierId && item.MaterialId == proposal.MaterialId
                && item.UoMId == proposal.UoMId
                && (!item.EffectiveTo.HasValue || item.EffectiveTo.Value.Date >= proposal.EffectiveFrom.Date)
                && (!proposal.EffectiveTo.HasValue || item.EffectiveFrom.Date <= proposal.EffectiveTo.Value.Date));
            if (overlap)
                return Error.Conflict("SupplierPricingAgreement.Overlap", "Another approved agreement now overlaps this proposal.");
        }
        if (proposal.ChangeKind == SupplierPricingAgreementChangeKind.Archive)
        {
            if (target is null)
                return Error.Conflict("SupplierPricingAgreement.Target", "Archive target is missing.");
            target.DeletedAt = DateTime.UtcNow;
            target.LastDeletedById = userId;
        }
        else if (target is not null)
        {
            if (proposal.EffectiveFrom.Date < target.EffectiveFrom.Date)
                return Error.Conflict("SupplierPricingAgreement.Dates", "Revision starts before the current agreement.");
            if (proposal.EffectiveFrom.Date == target.EffectiveFrom.Date)
            {
                target.DeletedAt = DateTime.UtcNow;
                target.LastDeletedById = userId;
            }
            else
            {
                var previousDayEnd = proposal.EffectiveFrom.Date.AddMilliseconds(-1);
                if (!target.EffectiveTo.HasValue || target.EffectiveTo.Value > previousDayEnd)
                    target.EffectiveTo = previousDayEnd;
                target.UpdatedAt = DateTime.UtcNow;
                target.LastUpdatedById = userId;
            }
        }
        proposal.Status = SupplierPricingAgreementStatus.Approved;
        return Result.Success();
    }
}
