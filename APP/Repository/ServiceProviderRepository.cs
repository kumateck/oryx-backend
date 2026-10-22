using APP.Extensions;
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
        var exists = await context.ServiceProviders
            .AnyAsync(sp => sp.Name == request.Name);

        if (exists)
            return Error.Validation("ServiceProvider.Exists", "Service Provider already exists");

        var services = await context.Services
            .Where(s => request.ServiceIds.Contains(s.Id))
            .ToListAsync();

        var missingIds = request.ServiceIds.Except(services.Select(s => s.Id)).ToList();
        if (missingIds.Count > 0)
            return Error.NotFound("Service.NotFound",
                $"Some services not found: {string.Join(", ", missingIds)}");

        var serviceProvider = mapper.Map<ServiceProvider>(request);

        serviceProvider.Services = services;

        await context.ServiceProviders.AddAsync(serviceProvider);
        await context.SaveChangesAsync();

        return serviceProvider.Id;
    }


    public async Task<Result<Paginateable<IEnumerable<ServiceProviderReducedDto>>>> GetServiceProviders(int page,
        int pageSize,
        string searchQuery,
        Guid? serviceId = null)
    {
        var query = context.ServiceProviders
            .AsSplitQuery()
            .Include(s => s.Country)
            .Include(s => s.Currency)
            .Include(s => s.Services)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, sp => sp.Name,
                sp => sp.Country.Name, sp => sp.Currency.Name, sp => sp.Email
                , sp => sp.Phone, sp => sp.Address);
        }

        if (serviceId.HasValue)
        {
            query = query.Where(q => q.Services.Any(s => s.Id == serviceId.Value));
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, 
            mapper.Map<ServiceProviderReducedDto>);
    }

    public async Task<Result<IEnumerable<ServiceProviderDto>>> GetServiceProvidersByService(Guid serviceId)
    {
        return mapper.Map<List<ServiceProviderDto>>(await context.ServiceProviders
            .AsSplitQuery()
            .Include(s => s.Country)
            .Include(s => s.Currency)
            .Include(s => s.Services)
            .Where(q => q.Services.Any(s => s.Id == serviceId))
            .ToListAsync());
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
        var serviceProvider = await context.ServiceProviders.Include(sp => sp.Services)
            .FirstOrDefaultAsync(sp => sp.Id == id);
        if (serviceProvider is null) return Error.NotFound("ServiceProvider.NotFound", "Service Provider not found");

        var services = await context.Services
            .Where(s => request.ServiceIds.Contains(s.Id))
            .ToListAsync();

        var missingIds = request.ServiceIds.Except(services.Select(s => s.Id)).ToList();
        if (missingIds.Count != 0)
            return Error.NotFound("Service.NotFound", $"Some services not found: {string.Join(", ", missingIds)}");

        mapper.Map(request, serviceProvider);
        serviceProvider.Services = services;
        context.ServiceProviders.Update(serviceProvider);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteServiceProvider(Guid id, Guid userId)
    {
        var serviceProvider = await context.ServiceProviders.FirstOrDefaultAsync(sp => sp.Id == id);
        if (serviceProvider is null)
            return Error.NotFound("ServiceProvider.NotFound", "Service Provider not found");

        // Preserve references for historical job, quotation and financial reconciliation.
        if (await context.JobOrderServiceProviders.IgnoreQueryFilters().AnyAsync(x => x.ServiceProviderId == id)
            || await context.JobOrderExecutions.IgnoreQueryFilters().AnyAsync(x => x.ServiceProviderId == id)
            || await context.ServiceQuotations.IgnoreQueryFilters().AnyAsync(x => x.ServiceProviderId == id)
            || await context.ServiceProformaInvoices.IgnoreQueryFilters().AnyAsync(x => x.ServiceProviderId == id)
            || await context.ServiceMemos.IgnoreQueryFilters().AnyAsync(x => x.ServiceProviderId == id))
            return Error.Validation("ServiceProvider.NotDeletable", "Service Provider is linked to a job, quotation, invoice or service memo");

        serviceProvider.DeletedAt = DateTime.UtcNow;
        serviceProvider.LastDeletedById = userId;

        context.ServiceProviders.Update(serviceProvider);
        await context.SaveChangesAsync();
        return Result.Success();

    }
}