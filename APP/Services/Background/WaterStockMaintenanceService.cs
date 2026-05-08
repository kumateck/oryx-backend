using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace APP.Services.Background;

public class WaterStockMaintenanceService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private const string WaterMaterialCode = "RMP020";
    private const string WaterBatchNumber = "WATER-UNLIMITED";
    private const decimal MaxQuantity = 999_999_999_999_999_999_999_999.99m;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var waterMaterialId = await dbContext.Materials
                    .Where(m => m.Code == WaterMaterialCode)
                    .Select(m => m.Id)
                    .FirstOrDefaultAsync(stoppingToken);

                if (waterMaterialId != Guid.Empty)
                {
                    var waterBatchId = await dbContext.MaterialBatches
                        .Where(b => b.MaterialId == waterMaterialId && b.BatchNumber == WaterBatchNumber)
                        .Select(b => b.Id)
                        .FirstOrDefaultAsync(stoppingToken);

                    if (waterBatchId != Guid.Empty)
                    {
                        // Reset batch properties: Max quantity, never expired, always available
                        await dbContext.MaterialBatches
                            .Where(b => b.Id == waterBatchId)
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(b => b.TotalQuantity, MaxQuantity)
                                .SetProperty(b => b.ConsumedQuantity, 0)
                                .SetProperty(b => b.Status, BatchStatus.Available)
                                .SetProperty(b => b.ExpiryDate, DateTime.MaxValue.ToUniversalTime()), stoppingToken);

                        // Reset shelf quantities to max
                        await dbContext.ShelfMaterialBatches
                            .Where(smb => smb.MaterialBatchId == waterBatchId)
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(smb => smb.Quantity, MaxQuantity), stoppingToken);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in WaterStockMaintenanceService: {e.Message}");
            }

            // Run every hour to ensure consistency
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
