using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace APP.Services.NotificationService;

public class NotificationDeliveryWorker(IServiceScopeFactory scopes, ILogger<NotificationDeliveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                for (var i = 0; i < 100 && !stoppingToken.IsCancellationRequested; i++)
                {
                    using var scope = scopes.CreateScope();
                    if (!await scope.ServiceProvider.GetRequiredService<AlertRequestDispatcher>().DispatchOne(DateTime.UtcNow, stoppingToken)) break;
                }
                // Fresh scope for each claim; do not retain a DbContext for the lifetime of the service.
                for (var i = 0; i < 100 && !stoppingToken.IsCancellationRequested; i++)
                {
                    using var scope = scopes.CreateScope();
                    if (!await scope.ServiceProvider.GetRequiredService<NotificationDeliveryDispatcher>().DispatchOne(DateTime.UtcNow, stoppingToken)) break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { logger.LogError("Notification dispatcher unavailable ({ErrorType}); retrying", error.GetType().Name); }
            try { await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}

/// <summary>SMTP has its own worker so a slow mail server cannot hold up real-time in-app publication.</summary>
public class NotificationEmailWorker(IServiceScopeFactory scopes, ILogger<NotificationEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<NotificationDeliveryDispatcher>().DispatchOne(DateTime.UtcNow, stoppingToken, emailOnly: true);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { logger.LogError("Email notification dispatcher unavailable ({ErrorType})", error.GetType().Name); }
            try { await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
