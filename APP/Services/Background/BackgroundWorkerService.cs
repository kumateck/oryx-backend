using System.Collections.Concurrent;
using APP.IRepository;
using APP.Services.NotificationService;
using DOMAIN.Entities.ActivityLogs;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Users;
using Microsoft.Extensions.Logging;
using JsonConvert = Newtonsoft.Json.JsonConvert;


namespace APP.Services.Background;
public class BackgroundWorkerService(ConcurrentQueue<CreateActivityLog> logQueue, ConcurrentQueue<(string message, NotificationType type, Guid? departmentId, List<User> users)> notificationQueue,
    ConcurrentQueue<PrevStateCaptureRequest> prevStateQueue, ILogger<BackgroundWorkerService> logger, IActivityLogRepository repo, ICollectionRepository collectionRepo, IAlertRepository alertRepo, AlertRequestStager alertStager)
    : IBackgroundWorkerService
{
    // Method to enqueue logs for background processing
    public void EnqueueLog(CreateActivityLog log)
    {
        logQueue.Enqueue(log);
    }

    public void EnqueueNotification(string message, NotificationType type, Guid? departmentId = null, List<User> users = null, Guid? shipmentDocumentId = null, string relatedEntityType = null, Guid? relatedEntityId = null, string relatedEntityCode = null, string relatedEntityName = null, string relatedEntityStatus = null, string relatedEntityPriority = null)
    {
        alertStager.Stage(message, type, departmentId, users, shipmentDocumentId: shipmentDocumentId, relatedEntityType: relatedEntityType, relatedEntityId: relatedEntityId, relatedEntityCode: relatedEntityCode, relatedEntityName: relatedEntityName, relatedEntityStatus: relatedEntityStatus, relatedEntityPriority: relatedEntityPriority);
    }

    public void EnqueuePrevStateCapture(PrevStateCaptureRequest prevStateCapture)
    {
        prevStateQueue.Enqueue(prevStateCapture);
    }

    // This method processes logs in the background
    public async Task DoWork(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logQueue.TryDequeue(out var log))
            {
                try
                {
                    // Asynchronously log activity
                    await repo.RecordActivityAsync(log);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing activity log.");
                }
            }

            if (prevStateQueue.TryDequeue(out var request))
            {
                try
                {
                    await HandlePrevStateCaptureAsync(request);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error capturing previous state.");
                }
            }

            if (notificationQueue.TryDequeue(out var notification))
            {
                try
                {
                    await alertRepo.ProcessAlert(notification.message, notification.type, notification.departmentId, notification.users);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing notification; returning it to the queue.");
                    notificationQueue.Enqueue(notification);
                }
            }

            // Delay to prevent excessive CPU usage
            await Task.Delay(notificationQueue.IsEmpty ? 1000 : 50, stoppingToken);
        }
    }

    private async Task HandlePrevStateCaptureAsync(PrevStateCaptureRequest request)
    {
        if (request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase)) return;

        switch (request.Model)
        {
            case "collections":
                if (request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
                {
                    var payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.RequestBody ?? "{}");

                    if (payload != null && payload.TryGetValue("id", out var idObj) && Guid.TryParse(idObj?.ToString(), out var id))
                    {
                        var entity = (await collectionRepo.GetItemCollection([], null)).Value;
                        if (entity != null)
                        {
                            var prevState = JsonConvert.SerializeObject(entity);
                            logger.LogInformation("Captured prev state for collection {Id}: {PrevState}", id, prevState);

                            // Optional: Save to a special log or attach to a CreateActivityLog object
                            // await repo.RecordPrevStateAsync(...);
                        }
                    }
                }
                break;

                // Add more models like this:
                // case "inventory": ...
        }
    }
}
