using APP.IRepository;
using APP.Services.Email;
using DOMAIN.Entities.ActivityLogs;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Notifications;
using INFRASTRUCTURE.Context;
using MassTransit;

namespace APP.Services.NotificationService;

public class NotificationService(IEmailService emailService, IPublishEndpoint publishEndpoint, IActivityLogRepository logRepository, ApplicationDbContext context) : INotificationService
{
    public async Task SendNotification(NotificationDto notification, List<AlertType> alertTypes)
    {
        foreach (var alertType in alertTypes)
        {
            switch (alertType)
            {
                case AlertType.InApp:
                    await publishEndpoint.Publish(notification);
                    await context.Notifications.AddAsync(new Notification
                    {
                        Id = notification.Id,
                        Message = notification.Message,
                        Recipients = notification.Recipients.Select(i => i.Id).ToList(),
                        Type = notification.Type,
                        AlertType = AlertType.InApp,
                        SentAt = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
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
                    await context.Notifications.AddAsync(new Notification
                    {
                        Id = notification.Id,
                        Message = notification.Message,
                        Recipients = notification.Recipients.Select(i => i.Id).ToList(),
                        Type = notification.Type,
                        AlertType = AlertType.Email,
                        SentAt = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
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