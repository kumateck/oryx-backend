using System.Collections.Concurrent;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace APP.Services.Background;

public class MaterialBatchExpiryService(IServiceScopeFactory scopeFactory, ConcurrentQueue<(string message, NotificationType type, Guid? departmentId, List<User> users)> notificationQueue) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var monthAgo = DateTime.UtcNow.AddMonths(-1);

                var expiredBatches = await dbContext.MaterialBatches
                    .Where(l => l.ExpiryDate < monthAgo)
                    .ToListAsync(stoppingToken);

                foreach (var batch in expiredBatches)
                {
                    notificationQueue.Enqueue(($"Material batch {batch.BatchNumber} expires at {batch.ExpiryDate:dd MMMM yyyy}", NotificationType.ExpiredMaterial,null, []));
                }
            
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}