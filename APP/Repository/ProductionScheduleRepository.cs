using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ProductionScheduleRepository(ApplicationDbContext context, IMapper mapper, UserManager<User> userManager) : IProductionScheduleRepository
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
            .Include(s => s.Products).ThenInclude(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        return productionSchedule is null ? Error.NotFound("ProductionSchedule.NotFound", "Production schedule is not found") : mapper.Map<ProductionScheduleDto>(productionSchedule);
    }
    
    public async Task<Result<List<ProductionScheduleProcurementDto>>> GetProductionScheduleDetail(Guid scheduleId, Guid userId)
    {
        // Fetch the production schedule with related data
        var productionSchedule = await context.ProductionSchedules
            .Include(s => s.Products).ThenInclude(s => s.Product)
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

        return new List<ProductionScheduleProcurementDto>();
    }
    

    public async Task<Result<Paginateable<IEnumerable<ProductionScheduleDto>>>> GetProductionSchedules(int page, int pageSize, string searchQuery) 
    { 
        var query = context.ProductionSchedules
            .Include(s => s.Products).ThenInclude(s => s.Product)
            .AsQueryable();
        
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

    public async Task<Result<Guid>> StartProductionActivity(Guid productionScheduleId, Guid productId)
    {

        if (context.ProductionActivities.Any(p =>
                p.ProductionScheduleId == productionScheduleId && p.ProductId == productId))
        {
            return Error.NotFound("ProductionActivity.AlreadyExist", "A production activity already exists for this product and schedule");

        }
        
        var productionSchedule =
            await context.ProductionSchedules.FirstOrDefaultAsync(p => p.Id == productionScheduleId);
        if(productionSchedule is null)
            return Error.NotFound("ProductionSchedule.NotFound", "Production schedule is not found");

        var product = await context.Products.Include(product => product.Routes).ThenInclude(route => route.Resources)
            .Include(product => product.Routes).ThenInclude(route => route.WorkCenters)
            .Include(product => product.Routes).ThenInclude(route => route.ResponsibleUsers)
            .Include(product => product.Routes).ThenInclude(route => route.ResponsibleRoles).FirstOrDefaultAsync(p => p.Id == productId);
        if(product is null)
            return Error.NotFound("Product.NotFound", "Product was not found");

        var users = product.Routes.SelectMany(r => r.ResponsibleUsers).Select(r => r.User).ToList();

        var roles = product.Routes.SelectMany(r => r.ResponsibleRoles).Select(r => r.Role).ToList();

        var usersInRole = new List<User>();

        foreach (var role in roles)
        {
            usersInRole.AddRange(await userManager.GetUsersInRoleAsync(role?.Name ?? ""));
        }

        var totalUsers = users.Concat(usersInRole).Distinct().ToList();

        var activity = new ProductionActivity
        {
            ProductId = productId,
            ProductionScheduleId = productionScheduleId,
            Code = Guid.NewGuid().ToString(),
            StartedAt = DateTime.UtcNow,
            Steps = product.Routes.Select(r => new ProductionActivityStep
            {
                OperationId = r.OperationId,
                WorkflowId = r.WorkflowId,
                Order = r.Order,
                Resources = r.Resources.Select(re => new ProductionActivityStepResource
                {
                    ResourceId = re.ResourceId
                }).ToList(),
                WorkCenters = r.WorkCenters.Select(re => new ProductionActivityStepWorkCenter
                {
                    WorkCenterId = re.WorkCenterId
                }).ToList(),
                ResponsibleUsers = totalUsers.Select(u => new ProductionActivityStepUser
                {
                    UserId = u.Id
                }).ToList()
            }).ToList()

        };

        await context.ProductionActivities.AddAsync(activity);
        await context.SaveChangesAsync();
        return activity.Id;
    }

    public async Task<Result> UpdateStatusOfProductionActivityStep(Guid productionStepId, ProductionStatus status, Guid userId)
    {
        var activityStep = await context.ProductionActivitySteps
            .Include(productionActivityStep => productionActivityStep.ResponsibleUsers)
            .Include(productionActivityStep => productionActivityStep.ProductionActivity).FirstOrDefaultAsync(p => p.Id == productionStepId);
        if(activityStep is null)
            return Error.NotFound("ProductActivity.NotFound", "Activity step was not found");

        if (activityStep.ResponsibleUsers.All(u => u.UserId != userId))
            return Error.Validation("ProductActivity.Validation", "You are not responsible for changing the status of this activity");
        
        activityStep.Status = status;

        switch (status)
        {
            case ProductionStatus.InProgress:
                activityStep.StartedAt = DateTime.UtcNow;
                
                bool isFirstStep = await context.ProductionActivitySteps
                    .Where(s => s.ProductionActivityId == activityStep.ProductionActivityId)
                    .OrderBy(s => s.Order)
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync() == productionStepId;

                if (isFirstStep)
                {
                    activityStep.ProductionActivity.Status = ProductionStatus.InProgress;
                }
                break;
            
            case ProductionStatus.Completed:
                activityStep.CompletedAt = DateTime.UtcNow;

                bool isLastStep = await context.ProductionActivitySteps
                    .Where(s => s.ProductionActivityId == activityStep.ProductionActivityId)
                    .OrderByDescending(s => s.Order)
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync() == productionStepId;

                if (isLastStep)
                {
                    activityStep.ProductionActivity.CompletedAt = DateTime.UtcNow;
                    activityStep.ProductionActivity.Status = ProductionStatus.Completed;
                }
                break;
        }

        context.ProductionActivitySteps.Update(activityStep);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result<Paginateable<IEnumerable<ProductionActivityDto>>>> GetProductionActivities(ProductionFilter filter)
    {
        var query = context.ProductionActivities
            .AsSplitQuery()
            .Include(pa => pa.ProductionSchedule)
            .Include(pa => pa.Product)
            .Include(pa => pa.Steps).ThenInclude(step => step.ResponsibleUsers)
            .Include(pa => pa.Steps).ThenInclude(step => step.Resources)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkCenters)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkFlow)
            .AsQueryable();

        if (filter.UserIds.Count != 0)
        {
            query = query.Where(pa => pa.Steps.Any(step => step.ResponsibleUsers.Any(ru => filter.UserIds.Contains(ru.UserId))));
        }

        if (filter.Statuses.Count != 0)
        {
            query = query.Where(pa => filter.Statuses.Contains(pa.Status));
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query, 
            filter,
            mapper.Map<ProductionActivityDto>
        );
    }
    
    public async Task<Result<ProductionActivityDto>> GetProductionActivityById(Guid productionActivityId)
    {
        var productionActivity = await context.ProductionActivities
            .AsSplitQuery()
            .Include(pa => pa.ProductionSchedule)
            .Include(pa => pa.Product)
            .Include(pa => pa.Steps).ThenInclude(step => step.ResponsibleUsers)
            .Include(pa => pa.Steps).ThenInclude(step => step.Resources)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkCenters)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkFlow)
            .FirstOrDefaultAsync(pa => pa.Id == productionActivityId);

        if (productionActivity is null)
            return Error.NotFound("ProductionActivity.NotFound", "Production activity not found");

        return Result.Success(mapper.Map<ProductionActivityDto>(productionActivity));
    }
    
    public async Task<Result<ProductionActivityDto>> GetProductionActivityByProductionScheduleIdAndProductId(Guid productionScheduleId, Guid productId)
    {
        var productionActivity = await context.ProductionActivities
            .AsSplitQuery()
            .Include(pa => pa.ProductionSchedule)
            .Include(pa => pa.Product)
            .Include(pa => pa.Steps).ThenInclude(step => step.ResponsibleUsers)
            .Include(pa => pa.Steps).ThenInclude(step => step.Resources)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkCenters)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkFlow)
            .FirstOrDefaultAsync(pa => pa.ProductionScheduleId == productionScheduleId && pa.ProductId == productId);

        if (productionActivity is null)
            return Error.NotFound("ProductionActivity.NotFound", "Production activity not found");

        return Result.Success(mapper.Map<ProductionActivityDto>(productionActivity));
    }
    
    public async Task<Result<Paginateable<IEnumerable<ProductionActivityStepDto>>>> GetProductionActivitySteps(ProductionFilter filter)
    {
        var query = context.ProductionActivitySteps
            .Include(pas => pas.ProductionActivity)
            .Include(pas => pas.ResponsibleUsers)
            .Include(pas => pas.Resources)
            .Include(pas => pas.WorkCenters)
            .AsQueryable();

        if (filter.UserIds.Count != 0)
        {
            query = query.Where(pas => pas.ResponsibleUsers.Any(ru => filter.UserIds.Contains(ru.UserId)));
        }

        if (filter.Statuses.Count != 0)
        {
            query = query.Where(pas => filter.Statuses.Contains(pas.Status));
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query, 
            filter,
            mapper.Map<ProductionActivityStepDto>
        );
    }
    
    public async Task<Result<ProductionActivityStepDto>> GetProductionActivityStepById(Guid productionActivityStepId)
    {
        var productionActivityStep = await context.ProductionActivitySteps
            .AsSplitQuery()
            .Include(pas => pas.ProductionActivity)
            .Include(pas => pas.ResponsibleUsers)
            .Include(pas => pas.Resources)
            .Include(pas => pas.WorkCenters)
            .Include(pas => pas.WorkFlow)
            .FirstOrDefaultAsync(pas => pas.Id == productionActivityStepId);

        if (productionActivityStep is null)
            return Error.NotFound("ProductionActivityStep.NotFound", "Production activity step not found");

        return Result.Success(mapper.Map<ProductionActivityStepDto>(productionActivityStep));
    }
    
    public async Task<Result<Dictionary<string, List<ProductionActivityDto>>>> GetProductionActivityGroupedByStatus()
    {
        var groupedData = await context.ProductionActivities
            .Include(pa => pa.ProductionSchedule)
            .Include(pa => pa.Product)
            .Include(pa => pa.Steps).ThenInclude(step => step.ResponsibleUsers)
            .Include(pa => pa.Steps).ThenInclude(step => step.Resources)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkCenters)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkFlow)
            .GroupBy(pas => pas.Status)
            .ToDictionaryAsync(
                g => g.Key.ToString(),
                g => g.Select(mapper.Map<ProductionActivityDto>).ToList()
            );

        return groupedData;
    }

    
    public async Task<Result<Dictionary<string, List<ProductionActivityStepDto>>>> GetProductionActivityStepsGroupedByStatus()
    {
        var groupedData = await context.ProductionActivitySteps
            .Include(pas => pas.ProductionActivity)
            .Include(pas => pas.ResponsibleUsers)
            .Include(pas => pas.Resources)
            .Include(pas => pas.WorkCenters)
            .Include(pas => pas.WorkFlow)
            .GroupBy(pas => pas.Status)
            .ToDictionaryAsync(
                g => g.Key.ToString(),
                g => g.Select(mapper.Map<ProductionActivityStepDto>).ToList()
            );

        return groupedData;
    }
}