using APP.Services.JobRequests;
using APP.Services.NotificationService;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.JobRequests;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Users;
using Xunit;

namespace APP.Tests.Repository;

public class JobRequestAssignmentNotifierTests
{
    [Fact]
    public async Task NotifyAssigned_TargetsTheSelectedEmployeeInApp()
    {
        var notificationService = new CapturingNotificationService();
        var notifier = new JobRequestAssignmentNotifier(notificationService);
        var assignee = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ama",
            LastName = "Mensah",
            Email = "ama@example.com",
        };

        await notifier.NotifyAssigned(assignee, new JobRequest
        {
            Code = "StJR0008",
            DescriptionOfWork = "Inspect the Vibro Sifter",
        });

        var notification = Assert.IsType<NotificationDto>(notificationService.Notification);
        Assert.Equal(NotificationType.JobRequestAssigned, notification.Type);
        Assert.Equal(assignee.Id, Assert.Single(notification.Recipients).Id);
        Assert.Contains("StJR0008", notification.Message);
        Assert.Equal([AlertType.InApp], notificationService.AlertTypes);
    }

    private sealed class CapturingNotificationService : INotificationService
    {
        public NotificationDto? Notification { get; private set; }
        public List<AlertType>? AlertTypes { get; private set; }

        public Task SendNotification(NotificationDto notification, List<AlertType> alertTypes)
        {
            Notification = notification;
            AlertTypes = alertTypes;
            return Task.CompletedTask;
        }
    }
}
