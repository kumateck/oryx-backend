using APP.Services.Email;
using APP.Services.Message;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Users;
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
                    break;
                
                case AlertType.Email:
                    break;
                
                case AlertType.Combination:
                    break;
            }
        }
    }

    public async Task SendNotification(string message, List<UserDto> users)
    {
        
    }
}