using APP.IRepository;
using APP.Services.Email;
using APP.Services.Message;
using DOMAIN.Entities.ActivityLogs;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Notifications;
using MassTransit;

namespace APP.Services.Notification;

public class NotificationService(IEmailService emailService, /*IMessagingService smsService,*/ IPublishEndpoint publishEndpoint, IActivityLogRepository logRepository) : INotificationService
{
    public async Task SendNotification(NotificationDto notification, List<AlertType> alertTypes)
    {
        foreach (var alertType in alertTypes)
        {
            switch (alertType)
            {
                case AlertType.InApp:
                    await publishEndpoint.Publish(notification);
                    foreach (var recipient in notification.Recipients)
                    { 
                        await logRepository.RecordActivityAsync(new CreateActivityLog
                        {
                            UserId = recipient.Id,
                            Action = "Received an in app alert",
                            Module = "Alert",
                            SubModule = "",
                            ActionType = ActionType.Read,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    break;
                
                case AlertType.Email:
                    emailService.ProcessNotificationData(notification);
                    foreach (var recipient in notification.Recipients)
                    { 
                        await logRepository.RecordActivityAsync(new CreateActivityLog
                        {
                            UserId = recipient.Id,
                            Action = "Received an email alert",
                            Module = "Alert",
                            SubModule = "",
                            ActionType = ActionType.Read,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    break;
            }
        }
    }
}