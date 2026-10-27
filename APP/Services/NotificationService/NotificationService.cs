using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Notifications;
using INFRASTRUCTURE.Context;

namespace APP.Services.NotificationService;

public class NotificationService(ApplicationDbContext context) : INotificationService
{
    public async Task SendNotification(NotificationDto notification, List<AlertType> alertTypes)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(alertTypes);
        if (string.IsNullOrWhiteSpace(notification.Message)) throw new ArgumentException("A notification requires a message.");
        if (notification.ShipmentDocumentId.HasValue && notification.RelatedEntityId == null)
        { notification.RelatedEntityType = "ShipmentDocument"; notification.RelatedEntityId = notification.ShipmentDocumentId; }
        ValidateRelatedEntity(notification);
        var recipients = notification.Recipients.Where(user => user.Id != Guid.Empty).DistinctBy(user => user.Id).Select(user => user.Id).ToList();
        if (recipients.Count == 0) return;
        var channels = alertTypes.Distinct().ToList();
        if (channels.Any(channel => !Enum.IsDefined(channel))) throw new ArgumentException("Invalid alert channel.");
        var now = DateTime.UtcNow;
        foreach (var channel in channels)
        {
            var stored = new Notification {
                Id = Guid.NewGuid(), Message = notification.Message, Type = notification.Type, ShipmentDocumentId = notification.ShipmentDocumentId, RelatedEntityType = notification.RelatedEntityType, RelatedEntityId = notification.RelatedEntityId, RelatedEntityCode = notification.RelatedEntityCode, RelatedEntityName = notification.RelatedEntityName, RelatedEntityStatus = notification.RelatedEntityStatus, RelatedEntityPriority = notification.RelatedEntityPriority,
                Recipients = recipients, AlertType = channel, SentAt = now,
            };
            context.Notifications.Add(stored);
            context.Set<NotificationDelivery>().Add(new NotificationDelivery {
                Id = stored.Id, NotificationId = stored.Id, Notification = stored, NextAttemptAt = now,
            });
            if (channel == AlertType.InApp || !channels.Contains(AlertType.InApp)) notification.Id = stored.Id;
        }
        // Notification and channel work are committed together. Neither RabbitMQ nor SMTP runs here.
        await context.SaveChangesAsync();
        notification.SentAt = now;
    }

    private static void ValidateRelatedEntity(NotificationDto notification)
    {
        if (string.IsNullOrWhiteSpace(notification.RelatedEntityType) != !notification.RelatedEntityId.HasValue)
            throw new ArgumentException("Related entity type and ID must be provided together.");
        if (notification.RelatedEntityType?.Length > 80 || notification.RelatedEntityCode?.Length > 128
            || notification.RelatedEntityName?.Length > 255 || notification.RelatedEntityStatus?.Length > 64
            || notification.RelatedEntityPriority?.Length > 32)
            throw new ArgumentException("Related entity metadata exceeds its supported length.");
    }
}
