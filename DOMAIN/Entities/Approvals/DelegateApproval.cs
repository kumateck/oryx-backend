using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.Approvals;

/// <summary>
/// A user's approvals handed to another user for a period. While it is active the delegate sees the
/// delegator's pending approvals and may decide them; the delegator keeps the right to decide them too.
/// Only the delegator's own approvals are handed over, never ones delegated to them, so delegations do
/// not chain. Revoking ends it at once; the record is kept.
/// </summary>
public class ApprovalDelegation : BaseEntity
{
    public Guid DelegatorId { get; set; }
    public User Delegator { get; set; }
    public Guid DelegateId { get; set; }
    public User Delegate { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    [StringLength(500)] public string Reason { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? RevokedById { get; set; }

    public bool IsActiveAt(DateTime now) => RevokedAt == null && DeletedAt == null && StartsAt <= now && now < EndsAt;

    public ApprovalDelegationStatus StatusAt(DateTime now) =>
        RevokedAt != null ? ApprovalDelegationStatus.Revoked
        : now < StartsAt ? ApprovalDelegationStatus.Upcoming
        : now < EndsAt ? ApprovalDelegationStatus.Active
        : ApprovalDelegationStatus.Ended;
}

public enum ApprovalDelegationStatus
{
    Upcoming = 0,
    Active = 1,
    Ended = 2,
    Revoked = 3,
}

/// <summary>Hands the caller's approvals to another user from <see cref="StartDate"/> until <see cref="EndDate"/>.</summary>
public class DelegateApproval
{
    [Required] public DateTime StartDate { get; set; }

    [Required] public DateTime EndDate { get; set; }

    /// <summary>The user who will decide the caller's approvals.</summary>
    [Required] public Guid DelegateUserId { get; set; }

    [StringLength(500)] public string Reason { get; set; }
}

public class ApprovalDelegationDto
{
    public Guid Id { get; set; }
    public CollectionItemDto Delegator { get; set; }
    public CollectionItemDto Delegate { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Reason { get; set; }
    public DateTime? RevokedAt { get; set; }
    public ApprovalDelegationStatus Status { get; set; }
}

/// <summary>The caller's delegations: those they gave, and those given to them.</summary>
public class MyApprovalDelegationsDto
{
    public List<ApprovalDelegationDto> Given { get; set; } = [];
    public List<ApprovalDelegationDto> Received { get; set; } = [];
}
