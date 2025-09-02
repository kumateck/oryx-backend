using System.Data.Entity;
using DOMAIN.Entities.ShiftSchedules;
using INFRASTRUCTURE.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace APP.Services.Background;

public class ShiftScheduleService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var today = DateTime.UtcNow;

            var shiftSchedules = await dbContext.ShiftSchedules
                .Where(s => s.ScheduleStatus != ScheduleStatus.Expired)
                .ToListAsync(stoppingToken);

            foreach (var shiftSchedule in shiftSchedules)
            {
                if (shiftSchedule.EndDate < today)
                {
                    shiftSchedule.ScheduleStatus = ScheduleStatus.Expired;
                }
                else if (shiftSchedule.StartDate <= today && shiftSchedule.EndDate >= today)
                {
                    shiftSchedule.ScheduleStatus = ScheduleStatus.InProgress;
                }
            }

            await dbContext.SaveChangesAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}