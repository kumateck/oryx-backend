using DOMAIN.Entities.Base;
using DOMAIN.Entities.ServiceProviders;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.JobRequests;

public class JobOrderExecutionDto : BaseDto
{
    public Guid JobOrderId { get; set; }
    public ServiceProviderDto ServiceProvider { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public JobOrderExecutionStatus Status { get; set; }
    public string Notes { get; set; }
    public List<JobActivityDto> Activities { get; set; } = [];
    public List<ConsumedItemDto> ConsumedItems { get; set; } = [];
    public DateTime? VerifiedAt { get; set; }
    public UserDto VerifiedBy { get; set; }
    public string VerificationComments { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public UserDto ApprovedBy { get; set; }
    public string ApprovalComments { get; set; }
    public bool RequesterSatisfied { get; set; }
    public string RequesterComments { get; set; }
}

