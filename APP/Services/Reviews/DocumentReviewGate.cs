using DOMAIN.Entities.Reviews;
using SHARED;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.Reviews;

/// <summary>
/// Opens reviews and holds approvals until a review is confirmed. It works on the caller's context so the
/// approval engine and the document repositories can use it without depending on the review service.
/// </summary>
public static class DocumentReviewGate
{
    public static Task<bool> IsConfiguredAsync(ApplicationDbContext context, string configurationType,
        CancellationToken cancellationToken = default) =>
        context.ReviewConfigurations.AnyAsync(item => item.ItemType == configurationType && item.Assignees.Any(), cancellationToken);

    public static Task<DocumentReview> LatestAsync(ApplicationDbContext context, string modelType, Guid modelId,
        CancellationToken cancellationToken = default) =>
        context.DocumentReviews.Where(item => item.ModelType == modelType && item.ModelId == modelId)
            .OrderByDescending(item => item.Submission).FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Called where a record's approvals would start. Returns true when they may start now. When the record's
    /// type has a review step and this submission has not been confirmed, it opens (or keeps) the review and
    /// returns false: nothing is approved, automatically or otherwise, until a reviewer confirms.
    /// </summary>
    public static async Task<bool> MayStartApprovalsAsync(ApplicationDbContext context, string modelType, Guid modelId,
        Guid? actorId, CancellationToken cancellationToken = default)
    {
        if (!ReviewDocumentTypes.HeldAtApprovalStart(modelType)) return true;
        var latest = await LatestAsync(context, modelType, modelId, cancellationToken);
        if (latest is { Status: DocumentReviewStatus.Confirmed, ApprovalsStartedAt: null })
        {
            latest.ApprovalsStartedAt = DateTime.UtcNow;
            latest.Version++;
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        if (latest is { Status: DocumentReviewStatus.AwaitingReview }) return false;
        if (latest is { Status: DocumentReviewStatus.Returned })
        {
            // Submitted again through the record's own flow: it goes back to its reviewers.
            latest.Status = DocumentReviewStatus.AwaitingReview;
            latest.ReviewedById = null;
            latest.ReviewedAt = null;
            Record(context, latest, DocumentReviewAction.Resubmitted, actorId, null);
            await context.SaveChangesAsync(cancellationToken);
            return false;
        }
        var configurationType = ReviewDocumentTypes.ConfigurationTypeFor(modelType);
        if (!ReviewDocumentTypes.AlwaysReviewedType(configurationType) && !await IsConfiguredAsync(context, configurationType, cancellationToken)) return true;
        var target = await ReviewDocumentTypes.LoadAsync(context, modelType, modelId, cancellationToken);
        if (target is null) return true;
        Open(context, target, latest, actorId);
        await context.SaveChangesAsync(cancellationToken);
        return false;
    }

    /// <summary>
    /// A record was prepared again (a purchase order finalised or revised): opens its review, or sends a
    /// returned one back for review. An edit to a record already awaiting review is recorded as an edit.
    /// </summary>
    public static async Task SubmitAsync(ApplicationDbContext context, string modelType, Guid modelId, Guid? actorId,
        CancellationToken cancellationToken = default)
    {
        var target = await ReviewDocumentTypes.LoadAsync(context, modelType, modelId, cancellationToken);
        if (target is null) return;
        var latest = await LatestAsync(context, modelType, modelId, cancellationToken);
        switch (latest?.Status)
        {
            case DocumentReviewStatus.AwaitingReview:
                Record(context, latest, DocumentReviewAction.Edited, actorId, null);
                break;
            case DocumentReviewStatus.Returned:
                latest.Status = DocumentReviewStatus.AwaitingReview;
                Record(context, latest, DocumentReviewAction.Resubmitted, actorId, null);
                break;
            default:
                if (!target.AlwaysReviewed && !await IsConfiguredAsync(context, target.ConfigurationType, cancellationToken)) return;
                Open(context, target, latest, actorId);
                break;
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Records a correction made while the record is in review. Does nothing when no review is open.</summary>
    public static async Task NoteEditAsync(ApplicationDbContext context, string modelType, Guid modelId, Guid? actorId,
        CancellationToken cancellationToken = default)
    {
        var latest = await LatestAsync(context, modelType, modelId, cancellationToken);
        if (latest is not { Status: DocumentReviewStatus.AwaitingReview or DocumentReviewStatus.Returned }) return;
        Record(context, latest, DocumentReviewAction.Edited, actorId, null);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>True while the record has a review that is awaiting review or returned.</summary>
    public static Task<bool> IsOpenAsync(ApplicationDbContext context, string modelType, Guid modelId,
        CancellationToken cancellationToken = default) =>
        context.DocumentReviews.AnyAsync(item => item.ModelType == modelType && item.ModelId == modelId
            && (item.Status == DocumentReviewStatus.AwaitingReview || item.Status == DocumentReviewStatus.Returned), cancellationToken);

    internal static DocumentReview Open(ApplicationDbContext context, ReviewTarget target, DocumentReview latest, Guid? actorId)
    {
        var review = new DocumentReview
        {
            ModelType = target.ModelType, ConfigurationType = target.ConfigurationType, ModelId = target.ModelId,
            Submission = (latest?.Submission ?? 0) + 1, Status = DocumentReviewStatus.AwaitingReview,
            PreparedById = target.PreparedById,
        };
        context.DocumentReviews.Add(review);
        Record(context, review, DocumentReviewAction.Submitted, actorId, null);
        return review;
    }

    internal static void Record(ApplicationDbContext context, DocumentReview review, DocumentReviewAction action, Guid? actorId, string comments)
    {
        review.Version++;
        context.DocumentReviewEvents.Add(new DocumentReviewEvent
            { DocumentReviewId = review.Id, Action = action, ActorId = actorId, Comments = comments });
    }

    public sealed record ReviewEligibility(bool Confirm, bool Return);

    /// <summary>
    /// When the type is configured, only its named roles and users may review, and the preparer may confirm
    /// their own record so work is not held up. Without a configuration (a purchase order, or a type whose
    /// configuration was removed while records were in review) the review keys alone decide.
    /// </summary>
    public static async Task<ReviewEligibility> EligibilityAsync(ApplicationDbContext context, ReviewTarget target, Guid actorId, HashSet<string> grants)
    {
        var confirmKeys = target.ConfirmKeys.All(grants.Contains);
        var returnKeys = target.ReturnKeys.All(grants.Contains);
        var assignees = await context.ReviewConfigurationAssignees.AsNoTracking()
            .Where(item => item.ReviewConfiguration.ItemType == target.ConfigurationType && item.ReviewConfiguration.DeletedAt == null)
            .Select(item => new { item.RoleId, item.UserId }).ToListAsync();
        if (assignees.Count == 0) return new(confirmKeys, returnKeys);
        var roles = await context.UserRoles.Where(item => item.UserId == actorId).Select(item => item.RoleId).ToListAsync();
        var named = assignees.Any(item => item.UserId == actorId || item.RoleId.HasValue && roles.Contains(item.RoleId.Value));
        return new(confirmKeys && (named || target.PreparedById == actorId), returnKeys && named);
    }


    /// <summary>
    /// Confirms a record's review as part of the record's own review action (a formula revision's "Record review",
    /// which needs its definition hash and a reason). Checks the review key and that the actor may review it, opens
    /// a review for a record from before reviews, and leaves the changes for the caller's save.
    /// </summary>
    public static async Task<Result> ConfirmForRecordAsync(ApplicationDbContext context, string modelType, Guid modelId,
        Guid actorId, string comments, CancellationToken cancellationToken = default)
    {
        var target = await ReviewDocumentTypes.LoadAsync(context, modelType, modelId, cancellationToken);
        if (target is null) return Error.NotFound("DocumentReview.NotFound", "The record was not found.");
        var grants = await APP.Services.Authorization.CurrentPermissionSet.Load(context, actorId);
        if (!target.ConfirmKeys.All(grants.Contains))
            return Error.Forbidden("DocumentReview.Permission", $"You need {string.Join(" and ", target.ConfirmKeys)} for this.");
        if (!(await EligibilityAsync(context, target, actorId, grants)).Confirm)
            return Error.Forbidden("DocumentReview.NotReviewer", "Only a reviewer named for this document type, or its preparer, can review it.");
        var review = await LatestAsync(context, target.ModelType, modelId, cancellationToken);
        if (review is { Status: DocumentReviewStatus.Returned })
            return Error.Conflict("DocumentReview.Returned", "This record was returned for correction. It must be sent back for review first.");
        if (review is not { Status: DocumentReviewStatus.AwaitingReview }) review = Open(context, target, review, target.PreparedById);
        review.Status = DocumentReviewStatus.Confirmed;
        review.ReviewedById = actorId;
        review.ReviewedAt = DateTime.UtcNow;
        review.Comments = string.IsNullOrWhiteSpace(comments) ? null : comments.Trim();
        review.ApprovalsStartedAt = review.ReviewedAt;
        Record(context, review, DocumentReviewAction.Confirmed, actorId, review.Comments);
        return Result.Success();
    }
}
