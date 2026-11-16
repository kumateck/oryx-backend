using System.ComponentModel.DataAnnotations;
using SHARED;

namespace DOMAIN.Entities.Reviews;

/// <summary>A document type that can be reviewed, for the settings page.</summary>
public record ReviewDocumentTypeDto(string ItemType, string Label, bool AlwaysReviewed);

public class ReviewConfigurationDto
{
    public Guid Id { get; set; }
    public string ItemType { get; set; }
    public string Label { get; set; }
    public List<ReviewAssigneeDto> Assignees { get; set; } = [];
}

/// <summary>Exactly one of role or user.</summary>
public class ReviewAssigneeDto
{
    public CollectionItemDto Role { get; set; }
    public CollectionItemDto User { get; set; }
}

public class SaveReviewConfigurationRequest
{
    [Required, StringLength(100)] public string ItemType { get; set; }
    public List<ReviewAssigneeRequest> Assignees { get; set; } = [];
}

/// <summary>Give exactly one of role or user.</summary>
public class ReviewAssigneeRequest
{
    public Guid? RoleId { get; set; }
    public Guid? UserId { get; set; }
}

/// <summary>A person on a review, with the stored signature that prints above their line.</summary>
public class ReviewPersonDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Signature { get; set; }
}

public class DocumentReviewEventDto
{
    public DocumentReviewAction Action { get; set; }
    public ReviewPersonDto Actor { get; set; }
    public string Comments { get; set; }
    public DateTime At { get; set; }
}

/// <summary>
/// The review of a record: the current or latest submission, its history, and what the caller may do.
/// <see cref="Review"/> is null when the record has never been reviewed.
/// </summary>
public class DocumentReviewStateDto
{
    public string ModelType { get; set; }
    public Guid ModelId { get; set; }
    public string Code { get; set; }
    /// <summary>True when this document type has a review step: it is configured, or always reviewed.</summary>
    public bool ReviewRequired { get; set; }
    public DocumentReviewDto Review { get; set; }
    public bool CanConfirm { get; set; }
    public bool CanReturn { get; set; }
    public bool CanResubmit { get; set; }
    /// <summary>Who prepared, checked and approved the record, for its prints.</summary>
    public DocumentSignaturesDto Signatures { get; set; }
}

public class DocumentReviewDto
{
    public Guid Id { get; set; }
    public int Submission { get; set; }
    public int Version { get; set; }
    public DocumentReviewStatus Status { get; set; }
    public ReviewPersonDto PreparedBy { get; set; }
    public DateTime SubmittedAt { get; set; }
    public ReviewPersonDto ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string Comments { get; set; }
    public List<DocumentReviewEventDto> Events { get; set; } = [];
}

/// <summary>
/// Prepared by is the record's creator; Checked by is whoever confirmed its latest confirmed review;
/// Approved by is the person who gave its final approval. A line nobody has signed is null.
/// </summary>
public class DocumentSignaturesDto
{
    public ReviewPersonDto PreparedBy { get; set; }
    public ReviewPersonDto CheckedBy { get; set; }
    public DateTime? CheckedAt { get; set; }
    public ReviewPersonDto ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

/// <summary>A record waiting for the caller's review, or returned to the caller to correct.</summary>
public class PendingReviewDto
{
    public Guid ReviewId { get; set; }
    public string ModelType { get; set; }
    public string DocumentLabel { get; set; }
    public Guid ModelId { get; set; }
    public string Code { get; set; }
    /// <summary>For a purchase order, its supplier type (0 foreign, 1 local), which its page needs.</summary>
    public int? Variant { get; set; }
    public DocumentReviewStatus Status { get; set; }
    public ReviewPersonDto PreparedBy { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string Comments { get; set; }
}

public class DocumentReviewActionRequest
{
    [StringLength(1000)] public string Comments { get; set; }
    /// <summary>The review version the caller saw, so a stale page cannot act on a newer submission.</summary>
    public int? Version { get; set; }
}
