namespace DOMAIN.Entities.Notifications;

/// <summary>Durable at-least-once in-app dispatch, written with the notification.</summary>
public class NotificationDelivery
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public NotificationDeliveryKind Kind { get; set; }
    public Guid? RecipientId { get; set; }
    public Notification Notification { get; set; }
    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public Guid LeaseId { get; set; }
    public DateTime? LeaseUntil { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string LastErrorCode { get; set; }
}

public enum NotificationDeliveryKind { Created = 0, Read = 1 }
public class NotificationReadEvent
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
}
