using DOMAIN.Entities.ActivityLogs;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Users;

namespace APP.Services.Background;

public interface IBackgroundWorkerService
{
    void EnqueueLog(CreateActivityLog log);
    void EnqueuePrevStateCapture(PrevStateCaptureRequest prevStateCaptureRequest);
    void EnqueueNotification(string message, NotificationType type, Guid? departmentId = null,
        List<User> users = null);
    Task DoWork(CancellationToken stoppingToken);
}