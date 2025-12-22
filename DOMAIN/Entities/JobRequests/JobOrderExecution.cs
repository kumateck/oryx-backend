using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ServiceProviders;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.JobRequests;

/// <summary>
/// Tracks the execution of externally assigned service jobs by contractors
/// </summary>
public class JobOrderExecution : BaseEntity
{
    public Guid JobOrderId { get; set; }
    public JobOrder JobOrder { get; set; }

    public Guid ServiceProviderId { get; set; }
    public ServiceProvider ServiceProvider { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public JobOrderExecutionStatus Status { get; set; } = JobOrderExecutionStatus.NotStarted;

    [StringLength(2000)]
    public string Notes { get; set; }

    // Activity tracking
    public List<JobActivity> Activities { get; set; } = [];

    // Items consumed during the service
    public List<ConsumedItem> ConsumedItems { get; set; } = [];

    // Verification by supervisor
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedById { get; set; }
    public User VerifiedBy { get; set; }

    [StringLength(1000)]
    public string VerificationComments { get; set; }

    // Approval by requester
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedById { get; set; }
    public User ApprovedBy { get; set; }

    [StringLength(1000)]
    public string ApprovalComments { get; set; }

    public bool RequesterSatisfied { get; set; }

    [StringLength(1000)]
    public string RequesterComments { get; set; }
}

public enum JobOrderExecutionStatus
{
    NotStarted,
    InProgress,
    Completed,
    VerifiedBySupervisor,
    ApprovedByRequester,
    Cancelled
}

