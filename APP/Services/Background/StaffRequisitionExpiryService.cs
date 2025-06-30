using DOMAIN.Entities.StaffRequisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace APP.Services.Background;

public class StaffRequisitionExpiryService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var today = DateTime.UtcNow;

            var expiredRequisitions = await dbContext.StaffRequisitions
                .Where(l => l.RequestUrgency < today && l.StaffRequisitionStatus == StaffRequisitionStatus.Approved && l.LastDeletedById == null)
                .ToListAsync(stoppingToken);

            foreach (var requisition in expiredRequisitions)
            {
                requisition.StaffRequisitionStatus = StaffRequisitionStatus.Expired;
            }

            await dbContext.SaveChangesAsync(stoppingToken);
            
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}