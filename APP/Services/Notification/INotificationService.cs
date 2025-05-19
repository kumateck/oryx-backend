using DOMAIN.Entities.Notifications;

namespace APP.Services.Notification;

public interface INotificationService
{
    Task<List<NotificationDto>> GetNotificationsForAllEntities(); 
}