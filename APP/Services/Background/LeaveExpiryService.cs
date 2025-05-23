using DOMAIN.Entities.LeaveRequests;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace APP.Services.Background;

public class LeaveExpiryService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var today = DateTime.UtcNow;

            var expiredLeaves = await dbContext.LeaveRequests
                .Where(l => l.EndDate < today && l.LeaveStatus == LeaveStatus.Approved && l.LastDeletedById == null)
                .ToListAsync(stoppingToken);

            foreach (var leave in expiredLeaves)
            {
                leave.LeaveStatus = LeaveStatus.Expired;
            }

            await dbContext.SaveChangesAsync(stoppingToken);
            
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}