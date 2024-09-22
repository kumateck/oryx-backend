using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Configurations;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ConfigurationRepository(ApplicationDbContext context, IMapper mapper) : IConfigurationRepository
{
    public async Task<Result<Guid>> CreateConfiguration(CreateConfigurationRequest request, Guid userId)
    {
        var configuration = mapper.Map<Configuration>(request);
        configuration.CreatedById = userId;
        await context.Configurations.AddAsync(configuration);
        await context.SaveChangesAsync();
        return Result.Success(configuration.Id);
    }

    public async Task<Result<ConfigurationDto>> GetConfiguration(Guid configurationId)
    {
        var configuration = await context.Configurations
            .FirstOrDefaultAsync(c => c.Id == configurationId);

        return configuration is null ? 
            Error.NotFound("Configuration.NotFound", "Configuration is not found") : 
            Result.Success(mapper.Map<ConfigurationDto>(configuration));
    }

    public async Task<Result<Paginateable<IEnumerable<ConfigurationDto>>>> GetConfigurations(int page, int pageSize, string searchQuery)
    {
        var query = context.Configurations.AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Prefix, q => q.ModelType);
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<ConfigurationDto>);
    }

    public async Task<Result> UpdateConfiguration(CreateConfigurationRequest request, Guid configurationId)
    {
        var existingConfiguration = await context.Configurations
            .FirstOrDefaultAsync(c => c.Id == configurationId);

        if (existingConfiguration == null)
        {
            return Error.NotFound("Configuration.NotFound", "Configuration is not found");
        }

        mapper.Map(request, existingConfiguration);
        context.Configurations.Update(existingConfiguration);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteConfiguration(Guid configurationId)
    {
        var configuration = await context.Configurations.FindAsync(configurationId);
        if (configuration == null)
        {
            return Error.NotFound("Configuration.NotFound", "Configuration is not found");
        }

        context.Configurations.Remove(configuration);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
