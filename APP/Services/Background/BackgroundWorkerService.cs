using System.Collections.Concurrent;
using APP.IRepository;
using DOMAIN.Entities.ActivityLogs;
using Microsoft.Extensions.Logging;
using JsonConvert = Newtonsoft.Json.JsonConvert;


namespace APP.Services.Background;
public class BackgroundWorkerService(ILogger<BackgroundWorkerService> logger, IActivityLogRepository repo, ICollectionRepository collectionRepo)
    : IBackgroundWorkerService
{
    private readonly ConcurrentQueue<CreateActivityLog> _logQueue = new();    
    private readonly ConcurrentQueue<PrevStateCaptureRequest> _prevStateQueue = new();

    // Method to enqueue logs for background processing
    public void EnqueueLog(CreateActivityLog log) 
    { 
        _logQueue.Enqueue(log);
    }
    
    public void EnqueuePrevStateCapture(string method, string model, string ipAddress, string userId, string requestBody = null)
    {
        _prevStateQueue.Enqueue(new PrevStateCaptureRequest
        {
            Method = method,
            Model = model?.ToLower(),
            IpAddress = ipAddress,
            UserId = userId,
            RequestBody = requestBody
        });
    }

    // This method processes logs in the background
    public async Task DoWork(CancellationToken stoppingToken) 
    { 
        while (!stoppingToken.IsCancellationRequested) 
        { 
            if (_logQueue.TryDequeue(out var log)) 
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
            
            if (_prevStateQueue.TryDequeue(out var request))
            {
                try
                {
                    //await HandlePrevStateCaptureAsync(request);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error capturing previous state.");
                }
            }

            // Delay to prevent excessive CPU usage
            await Task.Delay(5000, stoppingToken); // Poll every second for new logs
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
