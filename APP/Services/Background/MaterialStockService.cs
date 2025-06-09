using System.Collections.Concurrent;
using APP.IRepository;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace APP.Services.Background;

public class MaterialStockService(IServiceScopeFactory scopeFactory, ConcurrentQueue<(string message, NotificationType type, Guid? departmentId, List<User> users)> notificationQueue) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var materialRepository = scope.ServiceProvider.GetRequiredService<IMaterialRepository>();
                
                var materialDepartments = context.MaterialDepartments.AsSplitQuery()
                    .Include(materialDepartment => materialDepartment.Material).ToList();

                List<MaterialDepartment> materialBelowMinimumStockLevel = [];
                List<MaterialDepartment> materialAboveMaximumStockLevel = [];
                List<MaterialDepartment> materialAtReorderStockLevel = [];


                foreach (var materialDepartment in materialDepartments[..2])
                {
                    var departmentId = materialDepartment.DepartmentId;
                    var rawWarehouse = await context.Warehouses
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(w => w.DepartmentId == departmentId && w.Type == WarehouseType.RawMaterialStorage, cancellationToken: stoppingToken);
                    var packageMaterialWarehouse = await context.Warehouses
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(w => w.DepartmentId == departmentId && w.Type == WarehouseType.PackagedStorage, cancellationToken: stoppingToken);

                    var stockInWarehouseResult = materialDepartment.Material.Kind == MaterialKind.Raw
                        ? await materialRepository.GetMaterialStockInWarehouse(materialDepartment.MaterialId,
                            rawWarehouse.Id)
                        : await materialRepository.GetMaterialStockInWarehouse(materialDepartment.MaterialId,
                            packageMaterialWarehouse.Id);

                    if (stockInWarehouseResult.IsFailure) continue;
                    var stockInWarehouse = stockInWarehouseResult.Value;

                    if (stockInWarehouse < materialDepartment.MinimumStockLevel)
                    {
                        materialBelowMinimumStockLevel.Add(materialDepartment);
                    }

                    if (stockInWarehouse > materialDepartment.MaximumStockLevel)
                    {
                        materialAboveMaximumStockLevel.Add(materialDepartment);
                    }
                    
                    if (stockInWarehouse == materialDepartment.ReOrderLevel)
                    {
                        materialAtReorderStockLevel.Add(materialDepartment);
                    }
                }
                
                foreach (var material in materialBelowMinimumStockLevel)
                {
                    notificationQueue.Enqueue(($"Material {material.Material.Code} is below minimum stock level", NotificationType.MaterialBelowMinStock, material.DepartmentId, []));
                }

                foreach (var material in materialAboveMaximumStockLevel)
                {
                    notificationQueue.Enqueue(($"Material {material.Material.Code} stock is above maximum stock level", NotificationType.MaterialAboveMaxStock, material.DepartmentId, []));
                }
                
                foreach (var material in materialAtReorderStockLevel)
                {
                    notificationQueue.Enqueue(($"Material {material.Material.Code} stock is at reorder stock level", NotificationType.MaterialReachedReorderLevel, material.DepartmentId, []));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            await Task.Delay(TimeSpan.FromHours(8), stoppingToken);
        }
    }
}