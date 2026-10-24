using APP.IRepository;
using APP.Services.Email;
using DOMAIN.Entities.ActivityLogs;
using DOMAIN.Entities.Notifications;
using MassTransit;

namespace APP.Services.NotificationService;

public interface INotificationDeliveryEffects
{
    Task Publish(NotificationDto notification, CancellationToken cancellationToken);
    Task Read(NotificationReadEvent notification, CancellationToken cancellationToken);
    Task Email(NotificationDto notification, CancellationToken cancellationToken);
    Task Audit(Guid notificationId, Guid recipientId, CancellationToken cancellationToken);
}
public class NotificationDeliveryEffects(IPublishEndpoint publisher, IActivityLogRepository audit, IEmailService email) : INotificationDeliveryEffects
{
    public Task Publish(NotificationDto notification, CancellationToken cancellationToken) => publisher.Publish(notification, cancellationToken);
    public Task Read(NotificationReadEvent notification, CancellationToken cancellationToken) => publisher.Publish(notification, cancellationToken);
    public Task Email(NotificationDto notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        email.ProcessNotificationData(notification);
        return Task.CompletedTask;
    }
    public Task Audit(Guid notificationId, Guid recipientId, CancellationToken cancellationToken) => audit.RecordActivityAsync(new CreateActivityLog {
        UserId = recipientId, Action = "Alert channel delivered", Module = "Alert", SubModule = notificationId.ToString(),
        ActionType = ActionType.Create, CreatedAt = DateTime.UtcNow,
    });
}
