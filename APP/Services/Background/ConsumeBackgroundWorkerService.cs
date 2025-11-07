using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace APP.Services.Background;

public class ConsumeBackgroundWorkerService(IServiceProvider services, ILogger<ConsumeBackgroundWorkerService> logger)
    : BackgroundService
{
    public IServiceProvider Services { get; } = services;

    // Override ExecuteAsync to invoke DoWork in the background
    private async Task DoWork(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = Services.CreateScope();
            var scopedProcessingService =
                scope.ServiceProvider
                    .GetRequiredService<IBackgroundWorkerService>();
            await scopedProcessingService.DoWork(stoppingToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Consume Scoped Service Hosted Service running.");
        await DoWork(stoppingToken);
    }
}
