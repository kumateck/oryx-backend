using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.JobRequests;

public class JobActivityDto : BaseDto
{
    public Guid? JobExecutionId { get; set; }
    public Guid? JobOrderExecutionId { get; set; }
    public string ActivityDescription { get; set; }
    public DateTime PerformedAt { get; set; }
    public UserDto PerformedBy { get; set; }
    public string Notes { get; set; }
}

