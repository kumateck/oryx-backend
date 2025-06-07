using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Notifications;

namespace APP.Services.Notification;

public interface INotificationService
{
    Task SendNotification(NotificationDto notification, List<AlertType> alertTypes);
}