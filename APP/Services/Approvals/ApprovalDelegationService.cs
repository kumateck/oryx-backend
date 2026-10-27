using APP.IRepository;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.QcWorksheets;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Approvals;

/// <summary>Someone the caller may decide approvals for: themselves, or a user who delegated to them.</summary>
public sealed record ActingIdentity(Guid UserId, string Name, List<Guid> RoleIds);

/// <summary>A pending approval, and whose it is when the caller sees it only through a delegation.</summary>
public sealed record PendingForCaller(ApprovalEntity Entity, ActingIdentity OnBehalfOf);

public interface IApprovalDelegationService
{
    Task<Result<Guid>> Delegate(Guid delegatorId, DelegateApproval request, DateTime now);
    Task<MyApprovalDelegationsDto> GetMine(Guid userId, DateTime now);
    Task<Result> Revoke(Guid delegationId, Guid userId, DateTime now);
    Task<List<ActingIdentity>> ActiveDelegators(Guid delegateId, DateTime now);
    Task<List<PendingForCaller>> Pending(IApprovalRepository approvals, Guid userId, List<Guid> roleIds, string modelType, DateTime now);
    Task<string> NameOf(Guid userId);
}

/// <summary>
/// Approval delegation. The approval engine decides by (user, roles); a delegate acts with the delegator's
/// user and roles, so every document type follows its own rules unchanged. Electronically signed QC
/// documents are never delegated: their signature must be the signer's own.
/// </summary>
public sealed class ApprovalDelegationService(ApplicationDbContext context) : IApprovalDelegationService
{
    /// <summary>The longest a delegation may run; a longer absence is a change of approver, done by transferring rights.</summary>
    public static readonly TimeSpan MaximumLength = TimeSpan.FromDays(90);

    public static bool CanBeDelegated(string modelType) => !QcWorksheetModelTypes.IsQcWorksheetModelType(modelType);

    public async Task<Result<Guid>> Delegate(Guid delegatorId, DelegateApproval request, DateTime now)
    {
        if (request.DelegateUserId == delegatorId)
            return Error.Validation("ApprovalDelegation.Self", "You cannot delegate your approvals to yourself.");
        // A delegation that starts in the past starts now: decisions already taken stay the delegator's.
        var startsAt = request.StartDate < now ? now : request.StartDate;
        if (request.EndDate <= startsAt)
            return Error.Validation("ApprovalDelegation.Dates", "The delegation must end after it starts, and in the future.");
        if (request.EndDate - startsAt > MaximumLength)
            return Error.Validation("ApprovalDelegation.TooLong", $"A delegation can last at most {MaximumLength.TotalDays:0} days. For a longer absence, transfer approval rights.");

        var people = await context.Users.Where(user => user.Id == delegatorId || user.Id == request.DelegateUserId)
            .Select(user => new { user.Id, user.IsDisabled }).ToListAsync();
        var delegate_ = people.FirstOrDefault(user => user.Id == request.DelegateUserId);
        if (delegate_ is null) return Error.NotFound("ApprovalDelegation.DelegateNotFound", "The person to delegate to was not found.");
        if (delegate_.IsDisabled) return Error.Validation("ApprovalDelegation.DelegateDisabled", "The person to delegate to has a disabled account.");
        if (people.All(user => user.Id != delegatorId)) return Error.NotFound("ApprovalDelegation.DelegatorNotFound", "Your account was not found.");

        // One delegation at a time, so it is always clear who decides for whom.
        var overlaps = await context.ApprovalDelegations.AnyAsync(item => item.DelegatorId == delegatorId && item.RevokedAt == null
            && item.StartsAt < request.EndDate && startsAt < item.EndsAt);
        if (overlaps)
            return Error.Conflict("ApprovalDelegation.Overlap", "You already have a delegation for part of this period. Revoke it first.");

        var delegation = new ApprovalDelegation
        {
            Id = Guid.NewGuid(), DelegatorId = delegatorId, DelegateId = request.DelegateUserId, StartsAt = startsAt, EndsAt = request.EndDate,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(), CreatedById = delegatorId, CreatedAt = now,
        };
        context.ApprovalDelegations.Add(delegation);
        await context.SaveChangesAsync();
        return delegation.Id;
    }

    public async Task<MyApprovalDelegationsDto> GetMine(Guid userId, DateTime now)
    {
        // Ended delegations are listed for a month, so the delegator can see what was handed over recently.
        var since = now.AddDays(-30);
        var delegations = await context.ApprovalDelegations.AsNoTracking()
            .Where(item => (item.DelegatorId == userId || item.DelegateId == userId) && (item.EndsAt > since || (item.RevokedAt != null && item.RevokedAt > since)))
            .Select(item => new
            {
                item.Id, item.DelegatorId, item.DelegateId, item.StartsAt, item.EndsAt, item.Reason, item.RevokedAt,
                DelegatorName = item.Delegator.FirstName + " " + item.Delegator.LastName,
                DelegateName = item.Delegate.FirstName + " " + item.Delegate.LastName,
            })
            .OrderByDescending(item => item.StartsAt).ToListAsync();
        var mapped = delegations.Select(item => new ApprovalDelegationDto
        {
            Id = item.Id, StartsAt = item.StartsAt, EndsAt = item.EndsAt, Reason = item.Reason, RevokedAt = item.RevokedAt,
            Delegator = new CollectionItemDto { Id = item.DelegatorId, Name = item.DelegatorName.Trim() },
            Delegate = new CollectionItemDto { Id = item.DelegateId, Name = item.DelegateName.Trim() },
            Status = new ApprovalDelegation { StartsAt = item.StartsAt, EndsAt = item.EndsAt, RevokedAt = item.RevokedAt }.StatusAt(now),
        }).ToList();
        return new MyApprovalDelegationsDto
        {
            Given = mapped.Where(item => item.Delegator.Id == userId).ToList(),
            Received = mapped.Where(item => item.Delegate.Id == userId).ToList(),
        };
    }

    public async Task<Result> Revoke(Guid delegationId, Guid userId, DateTime now)
    {
        var delegation = await context.ApprovalDelegations.FirstOrDefaultAsync(item => item.Id == delegationId);
        // Either side may end it: the delegator takes their approvals back, or the delegate hands them back.
        if (delegation is null || (delegation.DelegatorId != userId && delegation.DelegateId != userId))
            return Error.NotFound("ApprovalDelegation.NotFound", "The delegation was not found.");
        if (delegation.RevokedAt != null || delegation.EndsAt <= now)
            return Error.Validation("ApprovalDelegation.Over", "This delegation has already ended.");
        delegation.RevokedAt = now;
        delegation.RevokedById = userId;
        delegation.LastUpdatedById = userId;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<List<ActingIdentity>> ActiveDelegators(Guid delegateId, DateTime now)
    {
        var delegators = await context.ApprovalDelegations.AsNoTracking()
            .Where(item => item.DelegateId == delegateId && item.RevokedAt == null && item.StartsAt <= now && now < item.EndsAt && !item.Delegator.IsDisabled)
            .Select(item => new { item.DelegatorId, Name = item.Delegator.FirstName + " " + item.Delegator.LastName })
            .Distinct().ToListAsync();
        if (delegators.Count == 0) return [];
        var ids = delegators.Select(item => item.DelegatorId).ToList();
        var roles = await context.UserRoles.AsNoTracking().Where(role => ids.Contains(role.UserId)).ToListAsync();
        return delegators.Select(item => new ActingIdentity(item.DelegatorId, item.Name.Trim(),
            roles.Where(role => role.UserId == item.DelegatorId).Select(role => role.RoleId).ToList())).ToList();
    }

    public async Task<List<PendingForCaller>> Pending(IApprovalRepository approvals, Guid userId, List<Guid> roleIds, string modelType, DateTime now)
    {
        var own = await approvals.GetEntitiesRequiringApproval(userId, roleIds ?? [], modelType);
        var result = own.Select(entity => new PendingForCaller(entity, null)).ToList();
        var seen = own.Select(entity => (entity.ModelType, entity.Id)).ToHashSet();
        foreach (var delegator in await ActiveDelegators(userId, now))
        {
            foreach (var entity in await approvals.GetEntitiesRequiringApproval(delegator.UserId, delegator.RoleIds, modelType))
            {
                if (!CanBeDelegated(entity.ModelType) || !seen.Add((entity.ModelType, entity.Id))) continue;
                entity.OnBehalfOf = new CollectionItemDto { Id = delegator.UserId, Name = delegator.Name };
                result.Add(new PendingForCaller(entity, delegator));
            }
        }
        return result;
    }

    public async Task<string> NameOf(Guid userId) =>
        (await context.Users.AsNoTracking().Where(user => user.Id == userId).Select(user => user.FirstName + " " + user.LastName).FirstOrDefaultAsync())?.Trim() ?? "a delegate";

    /// <summary>The comment kept with a decision taken for someone else, so the approval history says who decided.</summary>
    public static string OnBehalfComment(string actorName, ActingIdentity delegator, string comments) =>
        $"Decided by {actorName} on behalf of {delegator.Name} (delegated)." + (string.IsNullOrWhiteSpace(comments) ? "" : $" {comments.Trim()}");
}
