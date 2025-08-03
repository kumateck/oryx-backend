using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Materials.Batch;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace APP.Services.Background;

public class GrnStatusService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var grnList = await dbContext.Grns
                .Include(l => l.MaterialBatches)
                .ToListAsync(stoppingToken);

            foreach (var grn in grnList)
            {
                var batches = grn.MaterialBatches;

                if (batches == null || batches.Count == 0)
                {
                    grn.Status = Status.Pending;
                }
                else if (batches.All(b => b.Status == BatchStatus.Available))
                {
                    grn.Status = Status.Completed;
                }
                else if (batches.Any(b => b.Status == BatchStatus.Available))
                {
                    grn.Status = Status.Partial;
                }
                else
                {
                    grn.Status = Status.Pending;
                }
            }

            await dbContext.SaveChangesAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}