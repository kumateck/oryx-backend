using APP.Services.NotificationService;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.JobRequests;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Users;

namespace APP.Services.JobRequests;

public class JobRequestAssignmentNotifier(INotificationService notificationService)
    : IJobRequestAssignmentNotifier
{
    public Task NotifyAssigned(User assignee, JobRequest jobRequest)
    {
        var notification = new NotificationDto
        {
            Id = Guid.NewGuid(),
            Message = $"Job request {jobRequest.Code} was assigned to you: {jobRequest.DescriptionOfWork}",
            Recipients =
            [
                new UserDto
                {
                    Id = assignee.Id,
                    FirstName = assignee.FirstName,
                    LastName = assignee.LastName,
                    Email = assignee.Email,
                }
            ],
            Type = NotificationType.JobRequestAssigned,
            SentAt = DateTime.UtcNow,
        };

        return notificationService.SendNotification(notification, [AlertType.InApp]);
    }
}
