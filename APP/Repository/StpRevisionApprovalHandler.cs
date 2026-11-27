using System.Data;
using APP.Services.Authorization;
using APP.Services.Reviews;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Reviews;
using DOMAIN.Entities.StpRevisions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>
/// Approval stages of a structured STP revision. The decision key depends on the revision's stored kind, so it is
/// checked here rather than by the approval controller. The final approval makes the revision the current one of its
/// STP number and supersedes the revision it was revised from.
/// </summary>
internal static class StpRevisionApprovalHandler
{
    internal const string ModelType = ReviewDocumentTypes.StpRevision;

    internal static async Task CreateAsync(ApplicationDbContext context, Guid revisionId, List<ApprovalStage> stages, Approval approval)
    {
        if (await context.StpRevisionApprovals.AnyAsync(item => item.StpRevisionId == revisionId && item.ApprovalId == approval.Id)) return;
        await context.StpRevisionApprovals.AddRangeAsync(stages.Select((stage, index) => new StpRevisionApproval
        {
            Id = Guid.NewGuid(), StpRevisionId = revisionId, ApprovalId = approval.Id, Order = stage.Order,
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
        var revision = await context.StpRevisions.Include(item => item.Approvals).FirstOrDefaultAsync(item => item.Id == id);
        if (revision is null) return Error.NotFound("StpRevision.NotFound", "STP revision not found.");
        var key = StpRevisionPermissions.Key(decision == ApprovalStatus.Approved ? StpRevisionTask.Approve : StpRevisionTask.Reject, revision.Kind);
        if (!(await CurrentPermissionSet.Load(context, userId)).Contains(key))
            return Error.Forbidden("StpRevision.Permission", $"You need {key} for this.");
        if (revision.Status != StpRevisionStatus.Pending)
            return Error.Conflict("StpRevision.Status", "Only a revision awaiting approval can be approved or rejected.");
        var review = await DocumentReviewGate.LatestAsync(context, ModelType, id);
        if (review?.Status != DocumentReviewStatus.Confirmed)
            return Error.Conflict("StpRevision.Review", "The revision must pass review before an approval decision.");
        var stage = revision.Approvals.Where(item => item.Status == ApprovalStatus.Pending && item.ActivatedAt.HasValue)
            .OrderBy(item => item.Order).FirstOrDefault();
        if (stage is null) return Error.Conflict("StpRevision.Stage", "No approval stage is active.");
        if (stage.UserId != userId && (!stage.RoleId.HasValue || roleIds?.Contains(stage.RoleId.Value) != true))
            return Error.Validation("Approval.Unauthorized", "You are not assigned to this approval stage.");

        stage.Status = decision;
        stage.Comments = comments?.Trim();
        stage.ApprovalTime = DateTime.UtcNow;
        stage.ApprovedById = userId;
        var next = decision == ApprovalStatus.Approved
            ? revision.Approvals.Where(item => item.Status == ApprovalStatus.Pending && item.Order > stage.Order).OrderBy(item => item.Order).FirstOrDefault()
            : null;
        if (next is not null) next.ActivatedAt = DateTime.UtcNow;
        if (decision == ApprovalStatus.Rejected) revision.Status = StpRevisionStatus.Rejected;
        revision.UpdatedAt = DateTime.UtcNow;
        revision.LastUpdatedById = userId;
        context.ApprovalActionLogs.Add(new ApprovalActionLog { ModelId = revision.Id, UserId = userId, Status = decision, Comments = comments?.Trim() });
        await context.SaveChangesAsync();
        var allRequired = revision.Approvals.Where(item => item.Required).All(item => item.Status == ApprovalStatus.Approved);
        if (decision == ApprovalStatus.Approved && (next is null || allRequired))
        {
            var activation = await ActivateAsync(context, revision, userId);
            if (activation.IsFailure) return activation;
        }
        if (transaction is not null) await transaction.CommitAsync();
        return Result.Success();
    }

    /// <summary>Makes the revision the current one of its STP number and supersedes the one before it.</summary>
    internal static async Task<Result> ActivateAsync(ApplicationDbContext context, StpRevision revision, Guid? userId)
    {
        if (revision.Status != StpRevisionStatus.Pending)
            return Error.Conflict("StpRevision.Status", "The revision is not awaiting approval.");
        var current = await context.StpRevisions.FirstOrDefaultAsync(item =>
            item.Kind == revision.Kind && item.StpNumber == revision.StpNumber && item.Status == StpRevisionStatus.Approved);
        if (current is not null && current.Id != revision.RevisedFromId)
            return Error.Conflict("StpRevision.Current",
                "The approved revision of this STP changed since this revision was prepared. Prepare a new revision of the current one.");
        var now = DateTime.UtcNow;
        if (current is not null)
        {
            current.Status = StpRevisionStatus.Superseded;
            current.SupersededAt = now;
            current.UpdatedAt = now;
            current.LastUpdatedById = userId;
            // Saved first: at most one revision of an STP number may be approved at a time.
            await context.SaveChangesAsync();
        }
        revision.Status = StpRevisionStatus.Approved;
        revision.ApprovedAt = now;
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
