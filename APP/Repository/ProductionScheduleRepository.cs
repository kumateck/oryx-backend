using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ProductionScheduleRepository(ApplicationDbContext context, IMapper mapper, IMaterialRepository materialRepository) : IProductionScheduleRepository
{
     public async Task<Result<Guid>> CreateMasterProductionSchedule(CreateMasterProductionScheduleRequest request, Guid userId)
     { 
         var masterSchedule = mapper.Map<MasterProductionSchedule>(request); 
         masterSchedule.CreatedById = userId; 
         await context.MasterProductionSchedules.AddAsync(masterSchedule); 
         await context.SaveChangesAsync();
         return masterSchedule.Id;
     }

    public async Task<Result<MasterProductionScheduleDto>> GetMasterProductionSchedule(Guid masterScheduleId) 
    { 
        var masterSchedule = await context.MasterProductionSchedules
            .Include(ms => ms.WorkOrders)
            .FirstOrDefaultAsync(ms => ms.Id == masterScheduleId);

        return masterSchedule is null ? Error.NotFound("MasterProductionSchedule.NotFound", "Master Production schedule is not found")
                : mapper.Map<MasterProductionScheduleDto>(masterSchedule);
    }
    
    public async Task<Result<Paginateable<IEnumerable<MasterProductionScheduleDto>>>> GetMasterProductionSchedules(int page, int pageSize, string searchQuery) 
    { 
        var query = context.MasterProductionSchedules
            .AsSplitQuery()
            .Include(ms => ms.WorkOrders)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, f => f.Product.Name);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<MasterProductionScheduleDto>
        );
    }
    
    public async Task<Result> UpdateMasterProductionSchedule(UpdateMasterProductionScheduleRequest request, Guid masterScheduleId, Guid userId) 
    { 
        var existingSchedule = await context.MasterProductionSchedules
            .FirstOrDefaultAsync(ms => ms.Id == masterScheduleId);

        if (existingSchedule is null)
        {
            return Error.NotFound("MasterProductionSchedule.NotFound", "Master Production schedule is not found");
        }

        mapper.Map(request, existingSchedule);
        existingSchedule.LastUpdatedById = userId;

        context.MasterProductionSchedules.Update(existingSchedule);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result> DeleteMasterProductionSchedule(Guid masterScheduleId, Guid userId) 
    { 
        var schedule = await context.MasterProductionSchedules.FirstOrDefaultAsync(ms => ms.Id == masterScheduleId); 
        if (schedule is null)
        {
            return Error.NotFound("MasterProductionSchedule.NotFound", "Master Production schedule is not found");
        }

        schedule.DeletedAt = DateTime.UtcNow;
        schedule.LastDeletedById = userId;
        context.MasterProductionSchedules.Update(schedule);
        await context.SaveChangesAsync();
        return Result.Success();
    }
        
    
    public async Task<Result<Guid>> CreateProductionSchedule(CreateProductionScheduleRequest request, Guid userId) 
    { 
        var productionSchedule = mapper.Map<ProductionSchedule>(request); 
        productionSchedule.CreatedById = userId;
        await context.ProductionSchedules.AddAsync(productionSchedule); 
        await context.SaveChangesAsync();
        
        return productionSchedule.Id;
    }

    public async Task<Result<ProductionScheduleDto>> GetProductionSchedule(Guid scheduleId) 
    { 
        var productionSchedule = await context.ProductionSchedules
            .Include(s => s.Items).ThenInclude(s => s.Material)
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        return productionSchedule is null ? Error.NotFound("ProductionSchedule.NotFound", "Production schedule is not found") : mapper.Map<ProductionScheduleDto>(productionSchedule);
    }
    
    public async Task<Result<List<ProductionScheduleProcurementDto>>> GetProductionScheduleDetail(Guid scheduleId, Guid userId)
    {
        // Fetch the production schedule with related data
        var productionSchedule = await context.ProductionSchedules
            .Include(s => s.Items).ThenInclude(s => s.Material)
            .Include(s => s.Items).ThenInclude(s => s.UoM)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        if (productionSchedule is null)
            return Error.NotFound("ProductionSchedule.NotFound", "Production schedule is not found");

        // Fetch the user with related department data
        var user = await context.Users.Include(user => user.Department)
            .ThenInclude(u => u.Warehouses)
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user is null)
            return Error.NotFound("User.NotFound", $"User with id {userId} not found");

        // Initialize a dictionary to store stock levels
        var stockLevels = new Dictionary<Guid, decimal>();

        
        if (user.Department != null && user.Department.Warehouses.Count != 0)
        {
            var warehouseId = user.Department.Warehouses.FirstOrDefault(i => i.Warehouse.Type == WarehouseType.Production)?.WarehouseId;
            if (warehouseId.HasValue)
            {
                // Fetch stock levels for each material ID individually
                foreach (var materialId in productionSchedule.Items.Select(item => item.MaterialId).Distinct())
                {
                    var stockLevel = await materialRepository.GetMaterialStockInWarehouse(materialId, warehouseId.Value);
                    stockLevels[materialId] = stockLevel.Value;
                }
            }
        }
        
        // if (user.Department?.WarehouseId != null)
        // {
        //     var warehouseId = user.Department.WarehouseId.Value;
        //
        //     // Fetch stock levels for each material ID individually
        //     foreach (var materialId in productionSchedule.Items.Select(item => item.MaterialId).Distinct())
        //     {
        //         var stockLevel = await materialRepository.GetMaterialStockInWarehouse(materialId, warehouseId);
        //         stockLevels[materialId] = stockLevel.Value;
        //     }
        // }

        var procurementDetails = productionSchedule.Items.Select(item =>
        {
            var quantityOnHand = stockLevels.GetValueOrDefault(item.MaterialId, 0);

            return new ProductionScheduleProcurementDto
            {
                Material = mapper.Map<MaterialDto>(item.Material),
                UoM = mapper.Map<UnitOfMeasureDto>(item.UoM),
                QuantityRequested = item.Quantity,
                QuantityOnHand = quantityOnHand,
            };
        }).ToList();

        return procurementDetails;
    }
    

    public async Task<Result<Paginateable<IEnumerable<ProductionScheduleDto>>>> GetProductionSchedules(int page, int pageSize, string searchQuery) 
    { 
        var query = context.ProductionSchedules
            .Include(s => s.Items).ThenInclude(s => s.Material)
            .Include(s => s.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery)) 
        { 
            query = query.WhereSearch(searchQuery, f => f.Product.Name);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(
            query, 
            page, 
            pageSize, 
            mapper.Map<ProductionScheduleDto>);
    }
    
    public async Task<Result> UpdateProductionSchedule(UpdateProductionScheduleRequest request, Guid scheduleId, Guid userId) 
    { 
        var existingSchedule = await context.ProductionSchedules
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        if (existingSchedule is null) 
        { 
            return Error.NotFound("ProductionSchedule.NotFound", "Production schedule is not found");
        }
        
        mapper.Map(request, existingSchedule);
        existingSchedule.LastUpdatedById = userId;
        
        context.ProductionSchedules.Update(existingSchedule); 
        await context.SaveChangesAsync(); 
        return Result.Success();
    }
    
    public async Task<Result> DeleteProductionSchedule(Guid scheduleId, Guid userId) 
    { 
        var schedule = await context.ProductionSchedules.FirstOrDefaultAsync(s => s.Id == scheduleId); 
        if (schedule is null) 
        { 
            return Error.NotFound("ProductionSchedule.NotFound", "Production schedule is not found");
        }
        
        schedule.DeletedAt = DateTime.UtcNow; 
        schedule.LastDeletedById = userId; 
        context.ProductionSchedules.Update(schedule); 
        await context.SaveChangesAsync(); 
        return Result.Success();
    }
}