using DOMAIN.Entities.ActivityLogs;

namespace APP.Services.Background;

public interface IBackgroundWorkerService
{
    void EnqueueLog(CreateActivityLog log);
    void EnqueuePrevStateCapture(string method, string model, string ipAddress, string userId, string requestBody = null);
    Task DoWork(CancellationToken stoppingToken);
}