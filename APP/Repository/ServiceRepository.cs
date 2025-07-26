using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Services;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ServiceRepository(ApplicationDbContext context, IMapper mapper) : IServiceRepository
{
    public async Task<Result<Guid>> CreateService(CreateServiceRequest request)
    {
        var service = mapper.Map<Service>(request);
        await context.AddAsync(service);
        await context.SaveChangesAsync();
        return service.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<ServiceDto>>>> GetServices(int page, int pageSize,
        string searchQuery, bool? isActive = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = context.Services.AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Name);
        }
        
        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }
        
        if (startDate.HasValue)
        {
            query = query.Where(s => s.StartDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.EndDate <= endDate.Value);
        }


        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<ServiceDto>);
    }

    public async Task<Result<ServiceDto>> GetService(Guid id)
    {
       var service = await context.Services.FirstOrDefaultAsync(s => s.Id == id);
       return service is null ? 
           Error.NotFound("Service.NotFound", "Service not found") :
           mapper.Map<ServiceDto>(service, 
               opts => { opts.Items[AppConstants.ModelType] = nameof(Service);});
    }

    public async Task<Result> UpdateService(Guid id, CreateServiceRequest request)
    {
       var service = await context.Services.FirstOrDefaultAsync(s => s.Id == id);
       if (service == null) return Error.NotFound("Service.NotFound", "Service not found");
       
       mapper.Map(request, service);
       context.Services.Update(service);
       
       await context.SaveChangesAsync();
       return Result.Success();
    }

    public async Task<Result> DeleteService(Guid id, Guid userId)
    {
        var service = await context.Services.FirstOrDefaultAsync(s => s.Id == id);
        if (service == null) return Error.NotFound("Service.NotFound", "Service not found");
        
        //TODO: add service linking validation
        
        service.DeletedAt = DateTime.UtcNow;
        service.LastDeletedById = userId;
        
        context.Services.Update(service);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}