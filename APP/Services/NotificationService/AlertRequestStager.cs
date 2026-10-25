using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;

namespace APP.Services.NotificationService;

public class AlertRequestStager(ApplicationDbContext context)
{
    public void Stage(string message, NotificationType type, Guid? departmentId = null, List<User> users = null, string sourceKey = null, Guid? shipmentDocumentId = null, string relatedEntityType = null, Guid? relatedEntityId = null, string relatedEntityCode = null, string relatedEntityName = null, string relatedEntityStatus = null, string relatedEntityPriority = null)
    {
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("An alert requires a message.");
        if (shipmentDocumentId.HasValue && relatedEntityId == null) { relatedEntityType = "ShipmentDocument"; relatedEntityId = shipmentDocumentId; }
        relatedEntityCode = Bound(relatedEntityCode, 128);
        relatedEntityName = Bound(relatedEntityName, 255);
        relatedEntityStatus = Bound(relatedEntityStatus, 64);
        relatedEntityPriority = Bound(relatedEntityPriority, 32);
        ValidateContext(relatedEntityType, relatedEntityId, relatedEntityCode, relatedEntityName, relatedEntityStatus, relatedEntityPriority);
        // Never commit an in-progress business mutation just to enqueue its alert.
        var businessPending = context.ChangeTracker.HasChanges();
        context.Set<AlertDispatchRequest>().Add(new AlertDispatchRequest {
            SourceKey = sourceKey, Message = message, Type = type, DepartmentId = departmentId, ShipmentDocumentId = shipmentDocumentId, RelatedEntityType = relatedEntityType, RelatedEntityId = relatedEntityId, RelatedEntityCode = relatedEntityCode, RelatedEntityName = relatedEntityName, RelatedEntityStatus = relatedEntityStatus, RelatedEntityPriority = relatedEntityPriority,
            AssignedUserIds = (users ?? []).Select(user => user.Id).Where(id => id != Guid.Empty).Distinct().ToList(),
        });
        // Legacy producers sometimes call after their final save. In that case only the intent is saved.
        // Producers with pending changes commit the intent with their own normal SaveChanges/transaction.
        if (!businessPending) context.SaveChanges();
    }

    private static string Bound(string value, int length) => value?.Length > length ? value[..length] : value;

    private static void ValidateContext(string type, Guid? id, string code, string name, string status, string priority)
    {
        if (string.IsNullOrWhiteSpace(type) != !id.HasValue) throw new ArgumentException("Related entity type and ID must be provided together.");
        if (type?.Length > 80 || code?.Length > 128 || name?.Length > 255 || status?.Length > 64 || priority?.Length > 32) throw new ArgumentException("Related entity metadata exceeds its supported length.");
    }
}
