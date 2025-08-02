using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.ServiceProviders;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ServiceProviderRepository(ApplicationDbContext context, IMapper mapper) : IServiceProviderRepository
{
    public async Task<Result<Guid>> CreateServiceProvider(CreateServiceProviderRequest request)
    {
        var existingServiceProvider = await context.ServiceProviders
            .FirstOrDefaultAsync(sp => sp.Name == request.Name);
    
        if (existingServiceProvider != null)
            return Error.Validation("ServiceProvider.Exists", "Service Provider already exists");

        var validServiceIds = await context.Services
            .Where(s => request.ServiceIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        var missingIds = request.ServiceIds.Except(validServiceIds).ToList();
        if (missingIds.Count != 0)
            return Error.NotFound("Service.NotFound", $"Some services not found: {string.Join(", ", missingIds)}");

        var serviceProvider = mapper.Map<ServiceProvider>(request);
        await context.AddAsync(serviceProvider);
        await context.SaveChangesAsync();

        return serviceProvider.Id;
    }
    public async Task<Result<Paginateable<IEnumerable<ServiceProviderDto>>>> GetServiceProviders(int page, int pageSize, string searchQuery)
    {
        var query = context.ServiceProviders
            .AsSplitQuery()
            .Include(s => s.Country)
            .Include(s => s.Currency)
            .Include(s => s.Services)
            .AsQueryable();
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<ServiceProviderDto>);
    }

    public async Task<Result<ServiceProviderDto>> GetServiceProvider(Guid id)
    {
        var serviceProvider = await context.ServiceProviders
            .AsSplitQuery()
            .Include(s => s.Country)
            .Include(s => s.Currency)
            .Include(s => s.Services)
            .FirstOrDefaultAsync(sp => sp.Id == id);
        return serviceProvider is null ? 
            Error.NotFound("ServiceProvider.NotFound", "Service Provider not found") : 
            mapper.Map<ServiceProviderDto>(serviceProvider);
    }

    public async Task<Result> UpdateServiceProvider(Guid id, CreateServiceProviderRequest request)
    {
        var serviceProvider = await context.ServiceProviders.FirstOrDefaultAsync(sp => sp.Id == id);
        if (serviceProvider is null) return Error.NotFound("ServiceProvider.NotFound", "Service Provider not found");
        
        var validServiceIds = await context.Services
            .Where(s => request.ServiceIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        var missingIds = request.ServiceIds.Except(validServiceIds).ToList();
        if (missingIds.Count != 0)
            return Error.NotFound("Service.NotFound", $"Some services not found: {string.Join(", ", missingIds)}");
        
        mapper.Map(request, serviceProvider);
        context.ServiceProviders.Update(serviceProvider);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteServiceProvider(Guid id, Guid userId)
    {
        var serviceProvider = await context.ServiceProviders.FirstOrDefaultAsync(sp => sp.Id == id && sp.Services.Count > 0);
        if (serviceProvider != null) return Error.Validation("ServiceProvider.NotDeletable", "Service Provider is linked to inventory");
        
        serviceProvider.DeletedAt = DateTime.UtcNow;
        serviceProvider.LastDeletedById = userId;
                    
        context.ServiceProviders.Update(serviceProvider);
        await context.SaveChangesAsync();
        return Result.Success();

    }
}