using DOMAIN.Entities.Notifications;

namespace APP.Services.Notification;

public class NotificationService : INotificationService
{
    public Task<List<NotificationDto>> GetNotificationsForAllEntities()
    {
        throw new NotImplementedException();
    }
}