using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace APP.Services.Background;

public class ServiceExpiryService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var today = DateTime.UtcNow;

            var expiredServices = await dbContext.Services
                .Where(l => l.EndDate < today && l.LastDeletedById == null)
                .ToListAsync(stoppingToken);

            foreach (var service in expiredServices)
            {
                service.IsActive = false;
            }

            await dbContext.SaveChangesAsync(stoppingToken);
            
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}