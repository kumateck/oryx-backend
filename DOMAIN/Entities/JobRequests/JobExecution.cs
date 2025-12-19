using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.JobRequests;

/// <summary>
/// Tracks the execution of internally assigned service jobs
/// </summary>
public class JobExecution : BaseEntity
{
    public Guid JobRequestId { get; set; }
    public JobRequest JobRequest { get; set; }

    public Guid AssignedToEmployeeId { get; set; }
    public Employee AssignedToEmployee { get; set; }

    public DateTime AssignedAt { get; set; }
    public Guid AssignedById { get; set; }
    public User AssignedBy { get; set; }

    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public JobExecutionStatus Status { get; set; } = JobExecutionStatus.Assigned;

    [StringLength(2000)]
    public string Notes { get; set; }

    // Activity tracking
    public List<JobActivity> Activities { get; set; } = [];

    // Items consumed during the service
    public List<ConsumedItem> ConsumedItems { get; set; } = [];

    // Verification
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedById { get; set; }
    public User VerifiedBy { get; set; }

    [StringLength(1000)]
    public string VerificationComments { get; set; }

    // Approval
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedById { get; set; }
    public User ApprovedBy { get; set; }

    [StringLength(1000)]
    public string ApprovalComments { get; set; }
}

public enum JobExecutionStatus
{
    Assigned,
    Acknowledged,
    InProgress,
    Completed,
    VerifiedBySupervisor,
    Approved,
    Cancelled
}

