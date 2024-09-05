using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.WorkOrders;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class WorkOrderRepository(ApplicationDbContext context, IMapper mapper) : IWorkOrderRepository
{
    public async Task<Result<Guid>> CreateWorkOrder(CreateWorkOrderRequest request, Guid userId) 
    { 
        var workOrder = mapper.Map<WorkOrder>(request); 
        workOrder.CreatedById = userId; 
        await context.WorkOrders.AddAsync(workOrder);
        await context.ProductionSteps.AddRangeAsync(workOrder.Steps);
        await context.SaveChangesAsync();
        return workOrder.Id;
    }
    
    public async Task<Result<WorkOrderDto>> GetWorkOrder(Guid workOrderId) 
    { 
        var workOrder = await context.WorkOrders
            .Include(w => w.Steps)
            .Include(w => w.Product)
            .FirstOrDefaultAsync(w => w.Id == workOrderId);

        return workOrder is null ? Error.NotFound("WorkOrder.NotFound", "Work order is not found") : mapper.Map<WorkOrderDto>(workOrder);
    }

    public async Task<Result<Paginateable<IEnumerable<WorkOrderDto>>>> GetWorkOrders(int page, int pageSize, string searchQuery) 
    { 
        var query = context.WorkOrders
            .AsSplitQuery()
            .Include(w => w.Steps)
            .Include(w => w.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, f => f.Product.Name);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<WorkOrderDto>
        );
    }

    public async Task<Result> UpdateWorkOrder(UpdateWorkOrderRequest request, Guid workOrderId, Guid userId) 
    { 
        var existingWorkOrder = await context.WorkOrders.FirstOrDefaultAsync(w => w.Id == workOrderId);

        if (existingWorkOrder is null) 
        {
            return Error.NotFound("WorkOrder.NotFound", "Work order is not found");
        }

        mapper.Map(request, existingWorkOrder);
        existingWorkOrder.LastUpdatedById = userId;
        context.WorkOrders.Update(existingWorkOrder);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result> DeleteWorkOrder(Guid workOrderId, Guid userId) 
    { 
        var workOrder = await context.WorkOrders.FirstOrDefaultAsync(w => w.Id == workOrderId); 
        if (workOrder is null) 
        { 
            return Error.NotFound("WorkOrder.NotFound", "Work order is not found");
        }
        
        workOrder.DeletedAt = DateTime.UtcNow; 
        workOrder.LastDeletedById = userId; 
        context.WorkOrders.Update(workOrder); 
        await context.SaveChangesAsync(); 
        return Result.Success();
    }
}