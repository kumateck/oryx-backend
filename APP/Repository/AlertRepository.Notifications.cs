using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;
public partial class AlertRepository
{
    public async Task<Result<List<NotificationDto>>> GetNotificationsForUser(Guid userId, bool unreadOnly)
    {
        var query = context.Notifications
            .Where(n => n.AlertType == AlertType.InApp && n.Recipients.Contains(userId))
            .AsQueryable();

        if (unreadOnly)
        {
            query = query.Where(n =>
                !context.NotificationReads.Any(r => r.UserId == userId && r.NotificationId == n.Id));
        }

        var notifications = await query
            .OrderByDescending(n => n.SentAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Message = n.Message,
                Type = n.Type,
                SentAt = n.SentAt,
                ShipmentDocumentId = n.ShipmentDocumentId, RelatedEntityType = n.RelatedEntityType, RelatedEntityId = n.RelatedEntityId, RelatedEntityCode = n.RelatedEntityCode, RelatedEntityName = n.RelatedEntityName, RelatedEntityStatus = n.RelatedEntityStatus, RelatedEntityPriority = n.RelatedEntityPriority,
            })
            .ToListAsync();

        return Result.Success(notifications);
    }


    public async Task<Result> MarkNotificationAsRead(Guid id, Guid userId)
    {
        var notification = await context.Notifications.FirstOrDefaultAsync(item => item.Id == id && item.AlertType == AlertType.InApp);
        if (notification is null) return Error.NotFound("Notification", "Notification not found");

        if (!notification.Recipients.Contains(userId))
        {
            return Error.Validation("Notification.User", "You do not have permission to read this notification");
        }

        if (context.NotificationReads.Any(r => r.UserId == userId && r.NotificationId == notification.Id))
        {
            return Error.Validation("Notification.User", "You are already read this notification");
        }

        var readId = Guid.NewGuid();
        await context.NotificationReads.AddAsync(new NotificationRead
        {
            Id = readId,
            NotificationId = notification.Id,
            UserId = userId,
            ReadAt = DateTime.UtcNow
        });
        context.Set<NotificationDelivery>().Add(new NotificationDelivery {
            Id = readId, NotificationId = notification.Id, Notification = notification,
            Kind = NotificationDeliveryKind.Read, RecipientId = userId, NextAttemptAt = DateTime.UtcNow,
        });

        await context.SaveChangesAsync();
        return Result.Success();
    }
}
