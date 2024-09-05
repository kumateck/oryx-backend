using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.ProductionSchedules;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ProductionScheduleRepository(ApplicationDbContext context, IMapper mapper) : IProductionScheduleRepository
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
            .Include(s => s.WorkOrder)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        return productionSchedule is null ? Error.NotFound("ProductionSchedule.NotFound", "Production schedule is not found") : mapper.Map<ProductionScheduleDto>(productionSchedule);
    }
    
    public async Task<Result<Paginateable<IEnumerable<ProductionScheduleDto>>>> GetProductionSchedules(int page, int pageSize, string searchQuery) 
    { 
        var query = context.ProductionSchedules
            .AsSplitQuery()
            .Include(s => s.WorkOrder)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery)) 
        { 
            query = query.WhereSearch(searchQuery, f => f.WorkOrder.Product.Name);
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