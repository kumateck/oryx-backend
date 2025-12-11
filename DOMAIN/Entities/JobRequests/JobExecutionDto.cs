using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.JobRequests;

public class JobExecutionDto : BaseDto
{
    public Guid JobRequestId { get; set; }
    public EmployeeDto AssignedToEmployee { get; set; }
    public DateTime AssignedAt { get; set; }
    public UserDto AssignedBy { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public JobExecutionStatus Status { get; set; }
    public string Notes { get; set; }
    public List<JobActivityDto> Activities { get; set; } = [];
    public List<ConsumedItemDto> ConsumedItems { get; set; } = [];
    public DateTime? VerifiedAt { get; set; }
    public UserDto VerifiedBy { get; set; }
    public string VerificationComments { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public UserDto ApprovedBy { get; set; }
    public string ApprovalComments { get; set; }
}

