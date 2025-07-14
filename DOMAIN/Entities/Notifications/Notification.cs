using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Notifications;

public class Notification
{
    public Guid Id { get; set; }
    [StringLength(100000)] public string Message { get; set; }
    public List<Guid> Recipients { get; set; } = [];
    public NotificationType Type { get; set; }
    public AlertType AlertType { get; set; }
    public DateTime SentAt { get; set; }
}

public class NotificationRead
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public Notification Notification { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    public DateTime ReadAt { get; set; }
}