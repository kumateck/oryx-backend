using APP.Utils;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Users;
using SHARED;

namespace APP.IRepository;

public interface IAlertRepository
{ 
    Task<Result<Guid>> CreateAlert(CreateAlertRequest request);
    Task<Result<AlertDto>> GetAlert(Guid alertId);
    Task<Result<Paginateable<IEnumerable<AlertDto>>>> GetAlerts(int page, int pageSize, string searchQuery,
        bool withDisabled = false);
    Task<Result> UpdateAlert(CreateAlertRequest request, Guid userId, Guid approvalId);
    Task<Result> ToggleDisable(Guid id);
    Task<Result> DeleteAlert(Guid id, Guid userId);
    Task ProcessAlert(string message, NotificationType type, Guid? departmentId = null, List<User> assignedUsers = null);
    Task<Result> MarkNotificationAsRead(Guid id, Guid userId);
   Task<Result<List<NotificationDto>>> GetNotificationsForUser(Guid userId, bool unreadOnly);
}