using APP.Services.NotificationService;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace APP.Services.Background;

public class MaterialBatchExpiryService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var now = DateTime.UtcNow;
                var stager = scope.ServiceProvider.GetRequiredService<AlertRequestStager>();

                var expiredBatches = await dbContext.MaterialBatches
                    .Where(l => l.Status == BatchStatus.Available &&
                                l.ExpiryDate.HasValue && l.ExpiryDate < now)
                    .ToListAsync(stoppingToken);

                foreach (var batch in expiredBatches)
                {
                    var message = $"Material batch {batch.BatchNumber} expired at {batch.ExpiryDate:dd MMMM yyyy}";
                    var sourceKey = $"expired-batch:{batch.Id}:{batch.ExpiryDate:O}";
                    // Retained intent prevents daily repeats; a unique key also fences concurrent scanners.
                    if (!await dbContext.Set<AlertDispatchRequest>().AnyAsync(item => item.SourceKey == sourceKey, stoppingToken))
                        stager.Stage(message, NotificationType.ExpiredMaterial, sourceKey: sourceKey);
                }


            }
            catch (Exception e)
            {
                Console.WriteLine($"Expiry alert scan failed ({e.GetType().Name}); retrying after the scan interval.");
            }
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}