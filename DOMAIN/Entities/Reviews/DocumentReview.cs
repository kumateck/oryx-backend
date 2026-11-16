using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Reviews;

/// <summary>Who may review records of one document type before they go for approval.</summary>
public class ReviewConfiguration : BaseEntity
{
    /// <summary>The approval document type, as in <c>Approval.ItemType</c>.</summary>
    [StringLength(100)] public string ItemType { get; set; }
    public List<ReviewConfigurationAssignee> Assignees { get; set; } = [];
}

/// <summary>One role or one user named as a reviewer.</summary>
public class ReviewConfigurationAssignee
{
    public Guid Id { get; set; }
    public Guid ReviewConfigurationId { get; set; }
    public ReviewConfiguration ReviewConfiguration { get; set; }
    public Guid? RoleId { get; set; }
    public Role Role { get; set; }
    public Guid? UserId { get; set; }
    public User User { get; set; }
}

public enum DocumentReviewStatus
{
    AwaitingReview = 0,
    Returned = 1,
    Confirmed = 2,
}

public enum DocumentReviewAction
{
    Submitted = 0,
    Edited = 1,
    Returned = 2,
    Resubmitted = 3,
    Confirmed = 4,
}

/// <summary>
/// The review of one submission of a record: it is checked here, before its approvals start. A record
/// that is resubmitted after an approver rejects it gets a new review with the next submission number.
/// </summary>
public class DocumentReview
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>The model type the approval engine uses, such as <c>PurchaseOrder</c> or <c>RawStockRequisition</c>.</summary>
    [StringLength(100)] public string ModelType { get; set; }
    /// <summary>The document type its review configuration is kept under.</summary>
    [StringLength(100)] public string ConfigurationType { get; set; }
    public Guid ModelId { get; set; }
    public int Submission { get; set; }
    public DocumentReviewStatus Status { get; set; }
    public Guid? PreparedById { get; set; }
    public User PreparedBy { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public Guid? ReviewedById { get; set; }
    public User ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    /// <summary>The comment of the latest confirm or return.</summary>
    [StringLength(1000)] public string Comments { get; set; }
    /// <summary>When the confirmed review let the record's approvals start. Null until then.</summary>
    public DateTime? ApprovalsStartedAt { get; set; }
    /// <summary>Raised on every change, so two people acting at once cannot both succeed.</summary>
    public int Version { get; set; }
    public List<DocumentReviewEvent> Events { get; set; } = [];
}

/// <summary>An append-only line of a review's history.</summary>
public class DocumentReviewEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentReviewId { get; set; }
    public DocumentReview DocumentReview { get; set; }
    public DocumentReviewAction Action { get; set; }
    public Guid? ActorId { get; set; }
    public User Actor { get; set; }
    [StringLength(1000)] public string Comments { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
}
