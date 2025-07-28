using DOMAIN.Entities.Employees;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace APP.Services.Background;

public class EmployeeSuspensionService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            await dbContext.Employees
                .Where(e => e.Status == EmployeeStatus.Inactive && e.SuspensionEndDate < DateTime.UtcNow)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(e => e.Status, EmployeeStatus.Active), cancellationToken: stoppingToken);
            
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}