using APP.IRepository;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Reviews;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Reviews;

public interface IDocumentReviewService
{
    IReadOnlyList<ReviewDocumentTypeDto> Types();
    Task<Result<List<ReviewConfigurationDto>>> GetConfigurations(Guid actorId);
    Task<Result> SaveConfiguration(SaveReviewConfigurationRequest request, Guid actorId);
    Task<Result> DeleteConfiguration(string itemType, Guid actorId);
    Task<Result<DocumentReviewStateDto>> GetState(string modelType, Guid modelId, Guid actorId);
    Task<Result<Paginateable<IEnumerable<PendingReviewDto>>>> GetPending(Guid actorId, int page, int pageSize);
    Task<Result> Confirm(string modelType, Guid modelId, DocumentReviewActionRequest request, Guid actorId);
    Task<Result> Return(string modelType, Guid modelId, DocumentReviewActionRequest request, Guid actorId);
    Task<Result> Resubmit(string modelType, Guid modelId, DocumentReviewActionRequest request, Guid actorId);
}

public class DocumentReviewService(ApplicationDbContext context, IApprovalRepository approvals, IProcurementRepository procurement)
    : IDocumentReviewService
{
    private static readonly Error NotFound = Error.NotFound("DocumentReview.NotFound", "The record was not found.");
    private static readonly Error Stale = Error.Conflict("DocumentReview.Stale", "The review changed since you opened it. Reload and try again.");

    public IReadOnlyList<ReviewDocumentTypeDto> Types() => ReviewDocumentTypes.All;

    // ---------------- Configuration ----------------

    public async Task<Result<List<ReviewConfigurationDto>>> GetConfigurations(Guid actorId)
    {
        if (!(await CurrentPermissionSet.Load(context, actorId)).Contains(PermissionKeys.CanViewDocumentReviewSettings))
            return Error.Forbidden("DocumentReview.Permission", "You need CanViewDocumentReviewSettings for this.");
        var rows = await context.ReviewConfigurations.AsNoTracking().OrderBy(item => item.ItemType)
            .Select(item => new
            {
                item.Id, item.ItemType,
                Assignees = item.Assignees.Select(assignee => new
                {
                    assignee.RoleId, RoleName = assignee.Role.Name, assignee.UserId,
                    UserName = assignee.User.FirstName + " " + assignee.User.LastName,
                }).ToList(),
            }).ToListAsync();
        return rows.Select(item => new ReviewConfigurationDto
        {
            Id = item.Id, ItemType = item.ItemType, Label = ReviewDocumentTypes.Label(item.ItemType),
            Assignees = item.Assignees.Select(assignee => new ReviewAssigneeDto
            {
                Role = assignee.RoleId.HasValue ? new CollectionItemDto { Id = assignee.RoleId, Name = assignee.RoleName } : null,
                User = assignee.UserId.HasValue ? new CollectionItemDto { Id = assignee.UserId, Name = assignee.UserName.Trim() } : null,
            }).ToList(),
        }).ToList();
    }

    public async Task<Result> SaveConfiguration(SaveReviewConfigurationRequest request, Guid actorId)
    {
        var itemType = request?.ItemType?.Trim();
        if (string.IsNullOrEmpty(itemType) || !ReviewDocumentTypes.IsConfigurable(itemType))
            return Error.Validation("DocumentReview.Type", "Choose a document type that can be reviewed.");
        var assignees = request.Assignees ?? [];
        if (assignees.Count == 0)
            return Error.Validation("DocumentReview.Assignees", "Name at least one reviewer role or user.");
        if (assignees.Any(item => item.RoleId.HasValue == item.UserId.HasValue))
            return Error.Validation("DocumentReview.Assignee", "Each reviewer must be exactly one role or one user.");
        var roles = assignees.Where(item => item.RoleId.HasValue).Select(item => item.RoleId!.Value).ToList();
        var users = assignees.Where(item => item.UserId.HasValue).Select(item => item.UserId!.Value).ToList();
        if (roles.Distinct().Count() != roles.Count || users.Distinct().Count() != users.Count)
            return Error.Validation("DocumentReview.Duplicate", "Name each role or user only once.");
        if (await context.Roles.CountAsync(role => roles.Contains(role.Id)) != roles.Count
            || await context.Users.CountAsync(user => users.Contains(user.Id) && user.DeletedAt == null && !user.IsDisabled) != users.Count)
            return Error.Validation("DocumentReview.Unavailable", "Choose existing roles and active users.");

        if (!(await CurrentPermissionSet.Load(context, actorId)).Contains(PermissionKeys.CanEditDocumentReviewSettings))
            return Error.Forbidden("DocumentReview.Permission", "You need CanEditDocumentReviewSettings for this.");
        var configuration = await context.ReviewConfigurations.Include(item => item.Assignees)
            .FirstOrDefaultAsync(item => item.ItemType == itemType);
        if (configuration is null)
        {
            configuration = new ReviewConfiguration { Id = Guid.NewGuid(), ItemType = itemType, CreatedById = actorId };
            context.ReviewConfigurations.Add(configuration);
        }
        else
        {
            context.ReviewConfigurationAssignees.RemoveRange(configuration.Assignees);
            configuration.Assignees.Clear();
            configuration.LastUpdatedById = actorId;
            configuration.UpdatedAt = DateTime.UtcNow;
        }
        context.ReviewConfigurationAssignees.AddRange(assignees.Select(item => new ReviewConfigurationAssignee
            { Id = Guid.NewGuid(), ReviewConfigurationId = configuration.Id, RoleId = item.RoleId, UserId = item.UserId }));
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteConfiguration(string itemType, Guid actorId)
    {
        var configuration = await context.ReviewConfigurations.FirstOrDefaultAsync(item => item.ItemType == itemType);
        if (configuration is null) return Error.NotFound("DocumentReview.Configuration", "This document type has no review step.");
        if (!(await CurrentPermissionSet.Load(context, actorId)).Contains(PermissionKeys.CanDeleteDocumentReviewSettings))
            return Error.Forbidden("DocumentReview.Permission", "You need CanDeleteDocumentReviewSettings for this.");
        // Records already awaiting review stay in review; anyone holding the type's review key may finish them.
        configuration.DeletedAt = DateTime.UtcNow;
        configuration.LastDeletedById = actorId;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // ---------------- Reading ----------------

    public async Task<Result<DocumentReviewStateDto>> GetState(string modelType, Guid modelId, Guid actorId)
    {
        var target = await ReviewDocumentTypes.LoadAsync(context, modelType, modelId);
        if (target is null) return NotFound;
        var grants = await CurrentPermissionSet.Load(context, actorId);
        if (!target.ViewKeys.All(grants.Contains))
            return Error.Forbidden("DocumentReview.Permission", $"You need {string.Join(" and ", target.ViewKeys)} for this.");
        var latest = await context.DocumentReviews.AsNoTracking().Include(item => item.Events)
            .Where(item => item.ModelType == target.ModelType && item.ModelId == modelId)
            .OrderByDescending(item => item.Submission).FirstOrDefaultAsync();
        var confirmed = latest?.Status == DocumentReviewStatus.Confirmed ? latest
            : await context.DocumentReviews.AsNoTracking()
                .Where(item => item.ModelType == target.ModelType && item.ModelId == modelId && item.Status == DocumentReviewStatus.Confirmed)
                .OrderByDescending(item => item.Submission).FirstOrDefaultAsync();
        var approver = await ReviewDocumentTypes.FinalApproverAsync(context, target.ModelType, modelId);
        var people = await People([target.PreparedById, latest?.PreparedById, latest?.ReviewedById, confirmed?.ReviewedById, approver.UserId,
            .. latest?.Events.Select(item => item.ActorId) ?? []]);
        var eligibility = await Eligibility(target, actorId, grants);
        var awaiting = latest?.Status == DocumentReviewStatus.AwaitingReview;
        var legacy = await LegacyPurchaseOrderCheck(target);
        return new DocumentReviewStateDto
        {
            ModelType = target.ModelType, ModelId = modelId, Code = target.Code,
            ReviewRequired = target.AlwaysReviewed || await DocumentReviewGate.IsConfiguredAsync(context, target.ConfigurationType),
            Review = latest is null ? null : new DocumentReviewDto
            {
                Id = latest.Id, Submission = latest.Submission, Version = latest.Version, Status = latest.Status,
                PreparedBy = Person(people, latest.PreparedById), SubmittedAt = latest.SubmittedAt,
                ReviewedBy = Person(people, latest.ReviewedById), ReviewedAt = latest.ReviewedAt, Comments = latest.Comments,
                Events = latest.Events.OrderBy(item => item.At).Select(item => new DocumentReviewEventDto
                    { Action = item.Action, Actor = Person(people, item.ActorId), Comments = item.Comments, At = item.At }).ToList(),
            },
            CanConfirm = eligibility.Confirm && (awaiting || legacy),
            CanReturn = eligibility.Return && (awaiting || legacy),
            CanResubmit = latest?.Status == DocumentReviewStatus.Returned && target.ResubmitKeys.All(grants.Contains),
            Signatures = new DocumentSignaturesDto
            {
                PreparedBy = Person(people, target.PreparedById),
                CheckedBy = Person(people, confirmed?.ReviewedById), CheckedAt = confirmed?.ReviewedAt,
                ApprovedBy = Person(people, approver.UserId), ApprovedAt = approver.At,
            },
        };
    }

    public async Task<Result<Paginateable<IEnumerable<PendingReviewDto>>>> GetPending(Guid actorId, int page, int pageSize)
    {
        var grants = await CurrentPermissionSet.Load(context, actorId);
        if (!grants.Contains(PermissionKeys.CanViewPendingReviews))
            return Error.Forbidden("DocumentReview.Permission", "You need CanViewPendingReviews for this.");
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var open = await context.DocumentReviews.AsNoTracking()
            .Where(item => item.Status == DocumentReviewStatus.AwaitingReview || item.Status == DocumentReviewStatus.Returned)
            .OrderBy(item => item.SubmittedAt).ToListAsync();
        var rows = new List<(DocumentReview Review, ReviewTarget Target)>();
        foreach (var review in open)
        {
            var target = await ReviewDocumentTypes.LoadAsync(context, review.ModelType, review.ModelId);
            if (target is null || !target.ViewKeys.All(grants.Contains)) continue;
            var mine = review.Status == DocumentReviewStatus.Returned
                ? review.PreparedById == actorId && target.ResubmitKeys.All(grants.Contains)
                : (await Eligibility(target, actorId, grants)).Confirm;
            if (mine) rows.Add((review, target));
        }
        var slice = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var people = await People(slice.Select(row => row.Review.PreparedById).ToList());
        return new Paginateable<IEnumerable<PendingReviewDto>>
        {
            Data = slice.Select(row => new PendingReviewDto
            {
                ReviewId = row.Review.Id, ModelType = row.Review.ModelType, DocumentLabel = ReviewDocumentTypes.Label(row.Review.ConfigurationType),
                ModelId = row.Review.ModelId, Code = row.Target.Code, Variant = row.Target.Variant, Status = row.Review.Status,
                PreparedBy = Person(people, row.Review.PreparedById), SubmittedAt = row.Review.SubmittedAt, Comments = row.Review.Comments,
            }).ToList(),
            PageIndex = page, PageCount = (int)Math.Ceiling(rows.Count / (double)pageSize), TotalRecordCount = rows.Count,
            NumberOfPagesToShow = 5, StartPageIndex = 1, StopPageIndex = Math.Max(1, (int)Math.Ceiling(rows.Count / (double)pageSize)),
        };
    }

    // ---------------- Actions ----------------

    public async Task<Result> Confirm(string modelType, Guid modelId, DocumentReviewActionRequest request, Guid actorId)
    {
        var target = await ReviewDocumentTypes.LoadAsync(context, modelType, modelId);
        if (target is null) return NotFound;
        await using var transaction = context.Database.IsRelational() ? await context.Database.BeginTransactionAsync() : null;
        var review = await DocumentReviewGate.LatestAsync(context, target.ModelType, modelId);
        var grants = await CurrentPermissionSet.Load(context, actorId);
        if (!target.ConfirmKeys.All(grants.Contains))
            return Error.Forbidden("DocumentReview.Permission", $"You need {string.Join(" and ", target.ConfirmKeys)} for this.");
        if (!(await Eligibility(target, actorId, grants)).Confirm)
            return Error.Forbidden("DocumentReview.NotReviewer", "Only a reviewer named for this document type, or its preparer, can confirm its review.");
        var opened = false;
        if (review is not { Status: DocumentReviewStatus.AwaitingReview })
        {
            // An order finalised or revised before reviews existed is still waiting for its check.
            if (!await LegacyPurchaseOrderCheck(target)) return NotOpen(review);
            review = DocumentReviewGate.Open(context, target, review, target.PreparedById);
            opened = true;
        }
        if (Mismatch(review, request, opened)) return Stale;

        review.Status = DocumentReviewStatus.Confirmed;
        review.ReviewedById = actorId;
        review.ReviewedAt = DateTime.UtcNow;
        review.Comments = Clean(request?.Comments);
        DocumentReviewGate.Record(context, review, DocumentReviewAction.Confirmed, actorId, review.Comments);
        if (!await Save()) return Stale;

        if (target.ModelType == ReviewDocumentTypes.PurchaseOrder)
        {
            // The order's check: its prices are verified, it is marked checked and its approvals start.
            var checkedOrder = await procurement.CheckPurchaseOrder(modelId);
            if (checkedOrder.IsFailure) return checkedOrder;
            review.ApprovalsStartedAt = DateTime.UtcNow;
            review.Version++;
            await context.SaveChangesAsync();
        }
        else
        {
            await approvals.CreateInitialApprovalsAsync(target.ModelType, modelId, actorId);
        }
        if (transaction is not null) await transaction.CommitAsync();
        return Result.Success();
    }

    public async Task<Result> Return(string modelType, Guid modelId, DocumentReviewActionRequest request, Guid actorId)
    {
        var comments = Clean(request?.Comments);
        if (comments is null) return Error.Validation("DocumentReview.Comment", "Say what needs correcting.");
        var target = await ReviewDocumentTypes.LoadAsync(context, modelType, modelId);
        if (target is null) return NotFound;
        var review = await DocumentReviewGate.LatestAsync(context, target.ModelType, modelId);
        var grants = await CurrentPermissionSet.Load(context, actorId);
        if (!target.ReturnKeys.All(grants.Contains))
            return Error.Forbidden("DocumentReview.Permission", $"You need {string.Join(" and ", target.ReturnKeys)} for this.");
        if (!(await Eligibility(target, actorId, grants)).Return)
            return Error.Forbidden("DocumentReview.NotReviewer", "Only a reviewer named for this document type can return it.");
        var opened = false;
        if (review is not { Status: DocumentReviewStatus.AwaitingReview })
        {
            if (!await LegacyPurchaseOrderCheck(target)) return NotOpen(review);
            review = DocumentReviewGate.Open(context, target, review, target.PreparedById);
            opened = true;
        }
        if (Mismatch(review, request, opened)) return Stale;
        review.Status = DocumentReviewStatus.Returned;
        review.ReviewedById = actorId;
        review.ReviewedAt = DateTime.UtcNow;
        review.Comments = comments;
        DocumentReviewGate.Record(context, review, DocumentReviewAction.Returned, actorId, comments);
        return await Save() ? Result.Success() : Stale;
    }

    public async Task<Result> Resubmit(string modelType, Guid modelId, DocumentReviewActionRequest request, Guid actorId)
    {
        var target = await ReviewDocumentTypes.LoadAsync(context, modelType, modelId);
        if (target is null) return NotFound;
        var review = await DocumentReviewGate.LatestAsync(context, target.ModelType, modelId);
        if (review is not { Status: DocumentReviewStatus.Returned })
            return Error.Conflict("DocumentReview.NotReturned", "Only a record returned for correction can be sent back for review.");
        if (Mismatch(review, request, false)) return Stale;
        if (!(await CurrentPermissionSet.Load(context, actorId)).IsSupersetOf(target.ResubmitKeys))
            return Error.Forbidden("DocumentReview.Permission", $"You need {string.Join(" and ", target.ResubmitKeys)} for this.");
        review.Status = DocumentReviewStatus.AwaitingReview;
        review.ReviewedById = null;
        review.ReviewedAt = null;
        DocumentReviewGate.Record(context, review, DocumentReviewAction.Resubmitted, actorId, Clean(request?.Comments));
        return await Save() ? Result.Success() : Stale;
    }

    // ---------------- Helpers ----------------

    private sealed record ReviewEligibility(bool Confirm, bool Return);

    /// <summary>
    /// When the type is configured, only its named roles and users may review, and the preparer may confirm
    /// their own record so work is not held up. Without a configuration (a purchase order, or a type whose
    /// configuration was removed while records were in review) the review keys alone decide.
    /// </summary>
    private async Task<ReviewEligibility> Eligibility(ReviewTarget target, Guid actorId, HashSet<string> grants)
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

    /// <summary>A purchase order awaiting check with no open review: one finalised or revised before reviews existed.</summary>
    private async Task<bool> LegacyPurchaseOrderCheck(ReviewTarget target) =>
        target.ModelType == ReviewDocumentTypes.PurchaseOrder
        && !await DocumentReviewGate.IsOpenAsync(context, target.ModelType, target.ModelId)
        && await context.PurchaseOrders.AnyAsync(item => item.Id == target.ModelId
            && (item.Status == PurchaseOrderStatus.PendingCheck || item.Status == PurchaseOrderStatus.Revised));

    private static Error NotOpen(DocumentReview review) => review?.Status == DocumentReviewStatus.Returned
        ? Error.Conflict("DocumentReview.Returned", "This record was returned for correction. It must be sent back for review first.")
        : Error.Conflict("DocumentReview.NotOpen", "This record is not awaiting review.");

    /// <summary>The caller acted on an older state of this review. A review opened during this request has no earlier state.</summary>
    private static bool Mismatch(DocumentReview review, DocumentReviewActionRequest request, bool opened) =>
        !opened && request?.Version is { } version && review.Version != version;

    private static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<bool> Save()
    {
        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        catch (DbUpdateException)
        {
            // The open-review index refused a second review opened at the same moment.
            return false;
        }
    }

    private async Task<Dictionary<Guid, ReviewPersonDto>> People(IReadOnlyCollection<Guid?> ids)
    {
        var wanted = ids.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        return await context.Users.IgnoreQueryFilters().AsNoTracking().Where(user => wanted.Contains(user.Id))
            .Select(user => new ReviewPersonDto { Id = user.Id, Name = (user.FirstName + " " + user.LastName).Trim(), Signature = user.Signature })
            .ToDictionaryAsync(user => user.Id);
    }

    private static ReviewPersonDto Person(Dictionary<Guid, ReviewPersonDto> people, Guid? id) =>
        id.HasValue && people.TryGetValue(id.Value, out var person) ? person : null;
}
