namespace DOMAIN.Entities.Notifications;

/// <summary>Durable intent; stage with business changes so rollback also removes the alert.</summary>
public class AlertDispatchRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SourceKey { get; set; }
    public string Message { get; set; }
    public NotificationType Type { get; set; }
    public Guid? ShipmentDocumentId { get; set; }
    public Guid? DepartmentId { get; set; }
    public List<Guid> AssignedUserIds { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;
    public Guid LeaseId { get; set; }
    public DateTime? LeaseUntil { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int Attempts { get; set; }
    public string LastErrorCode { get; set; }
}
