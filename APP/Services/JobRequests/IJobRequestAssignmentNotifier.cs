using DOMAIN.Entities.JobRequests;
using DOMAIN.Entities.Users;

namespace APP.Services.JobRequests;

public interface IJobRequestAssignmentNotifier
{
    Task NotifyAssigned(User assignee, JobRequest jobRequest);
}
