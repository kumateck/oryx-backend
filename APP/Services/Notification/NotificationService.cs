using APP.Services.Email;
using APP.Services.Message;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Notifications;
using MassTransit;

namespace APP.Services.Notification;

public class NotificationService(IEmailService emailService, IMessagingService smsService, IPublishEndpoint publishEndpoint) : INotificationService
{
    public async Task SendNotification(NotificationDto notification, List<AlertType> alertTypes)
    {
        foreach (var alertType in alertTypes)
        {
            switch (alertType)
            {
                case AlertType.InApp:
                    await publishEndpoint.Publish(notification);
                    break;
                
                case AlertType.Email:
                    emailService.ProcessNotificationData(notification);
                    break;
                
                case AlertType.Sms:
                    break;
            }
        }
    }
}