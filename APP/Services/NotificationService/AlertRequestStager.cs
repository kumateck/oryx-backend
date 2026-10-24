using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;

namespace APP.Services.NotificationService;

public class AlertRequestStager(ApplicationDbContext context)
{
    public void Stage(string message, NotificationType type, Guid? departmentId = null, List<User> users = null, string sourceKey = null)
    {
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("An alert requires a message.");
        // Never commit an in-progress business mutation just to enqueue its alert.
        var businessPending = context.ChangeTracker.HasChanges();
        context.Set<AlertDispatchRequest>().Add(new AlertDispatchRequest {
            SourceKey = sourceKey, Message = message, Type = type, DepartmentId = departmentId,
            AssignedUserIds = (users ?? []).Select(user => user.Id).Where(id => id != Guid.Empty).Distinct().ToList(),
        });
        // Legacy producers sometimes call after their final save. In that case only the intent is saved.
        // Producers with pending changes commit the intent with their own normal SaveChanges/transaction.
        if (!businessPending) context.SaveChanges();
    }
}
