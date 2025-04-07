using DOMAIN.Entities.ActivityLogs;

namespace APP.Services.Background;

public interface IBackgroundWorkerService
{
    void EnqueueLog(CreateActivityLog log);
    void EnqueuePrevStateCapture(PrevStateCaptureRequest prevStateCaptureRequest);
    Task DoWork(CancellationToken stoppingToken);
}