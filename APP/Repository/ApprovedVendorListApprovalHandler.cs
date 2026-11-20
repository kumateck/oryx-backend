using System.Data;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.ApprovedVendorLists;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>Approval stages of an approved vendor list. Its final approval makes it the current list.</summary>
internal static class ApprovedVendorListApprovalHandler
{
    internal const string ModelType = "ApprovedVendorList";

    internal static async Task CreateAsync(ApplicationDbContext context, Guid listId, List<ApprovalStage> stages, Approval approval)
    {
        if (await context.ApprovedVendorListApprovals.AnyAsync(item => item.ApprovedVendorListId == listId && item.ApprovalId == approval.Id)) return;
        await context.ApprovedVendorListApprovals.AddRangeAsync(stages.Select((stage, index) => new ApprovedVendorListApproval
        {
            Id = Guid.NewGuid(), ApprovedVendorListId = listId, ApprovalId = approval.Id, Order = stage.Order,
            Required = stage.Required, UserId = stage.UserId, RoleId = stage.RoleId,
            ActivatedAt = index == 0 ? DateTime.UtcNow : null, CreatedAt = DateTime.UtcNow,
        }));
        await context.SaveChangesAsync();
    }

    internal static Task<Result> ApproveAsync(ApplicationDbContext context, Guid id, Guid userId, List<Guid> roleIds, string comments) =>
        DecideAsync(context, id, userId, roleIds, comments, ApprovalStatus.Approved);

    internal static Task<Result> RejectAsync(ApplicationDbContext context, Guid id, Guid userId, List<Guid> roleIds, string comments) =>
        DecideAsync(context, id, userId, roleIds, comments, ApprovalStatus.Rejected);

    private static async Task<Result> DecideAsync(ApplicationDbContext context, Guid id, Guid userId, List<Guid> roleIds,
        string comments, ApprovalStatus decision)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var list = await context.ApprovedVendorLists.Include(item => item.Approvals).FirstOrDefaultAsync(item => item.Id == id);
        if (list is null) return Error.NotFound("ApprovedVendorList.NotFound", "Approved vendor list not found.");
        if (list.Status != ApprovedVendorListStatus.Pending)
            return Error.Conflict("ApprovedVendorList.Status", "Only a list awaiting approval can be approved or rejected.");
        var stage = list.Approvals.Where(item => item.Status == ApprovalStatus.Pending && item.ActivatedAt.HasValue)
            .OrderBy(item => item.Order).FirstOrDefault();
        if (stage is null) return Error.Conflict("ApprovedVendorList.Stage", "No approval stage is active.");
        if (stage.UserId != userId && (!stage.RoleId.HasValue || roleIds?.Contains(stage.RoleId.Value) != true))
            return Error.Validation("Approval.Unauthorized", "You are not assigned to this approval stage.");

        stage.Status = decision;
        stage.Comments = comments?.Trim();
        stage.ApprovalTime = DateTime.UtcNow;
        stage.ApprovedById = userId;
        var next = decision == ApprovalStatus.Approved
            ? list.Approvals.Where(item => item.Status == ApprovalStatus.Pending && item.Order > stage.Order).OrderBy(item => item.Order).FirstOrDefault()
            : null;
        if (next is not null) next.ActivatedAt = DateTime.UtcNow;
        if (decision == ApprovalStatus.Rejected) list.Status = ApprovedVendorListStatus.Rejected;
        list.UpdatedAt = DateTime.UtcNow;
        list.LastUpdatedById = userId;
        context.ApprovalActionLogs.Add(new ApprovalActionLog { ModelId = list.Id, UserId = userId, Status = decision, Comments = comments?.Trim() });
        await context.SaveChangesAsync();
        var allRequired = list.Approvals.Where(item => item.Required).All(item => item.Status == ApprovalStatus.Approved);
        if (decision == ApprovalStatus.Approved && (next is null || allRequired))
        {
            var activation = await ActivateAsync(context, list, userId);
            if (activation.IsFailure) return activation;
        }
        if (transaction is not null) await transaction.CommitAsync();
        return Result.Success();
    }

    /// <summary>Makes the list the current approved list and supersedes the one before it.</summary>
    internal static async Task<Result> ActivateAsync(ApplicationDbContext context, ApprovedVendorList list, Guid? userId)
    {
        if (list.Status != ApprovedVendorListStatus.Pending)
            return Error.Conflict("ApprovedVendorList.Status", "The list is not awaiting approval.");
        var current = await context.ApprovedVendorLists.FirstOrDefaultAsync(item => item.Status == ApprovedVendorListStatus.Approved);
        if (current is not null && current.Id != list.PreviousRevisionId)
            return Error.Conflict("ApprovedVendorList.Current", "The approved list changed since this revision was prepared. Prepare a new revision of the current list.");
        var now = DateTime.UtcNow;
        if (current is not null)
        {
            current.Status = ApprovedVendorListStatus.Superseded;
            current.SupersededAt = now;
            current.UpdatedAt = now;
            current.LastUpdatedById = userId;
            // Saved first: at most one list may be approved at a time.
            await context.SaveChangesAsync();
        }
        list.Status = ApprovedVendorListStatus.Approved;
        list.ApprovedAt = now;
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
