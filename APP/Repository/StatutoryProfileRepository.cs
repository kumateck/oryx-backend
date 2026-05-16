using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.StatutoryProfiles;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class StatutoryProfileRepository(ApplicationDbContext context, IMapper mapper) : IStatutoryProfileRepository
{
    public async Task<Result<Guid>> CreateStatutoryProfile(CreateStatutoryProfileRequest request)
    {
        var country = await context.Countries.AnyAsync(c => c.Id == request.CountryId);
        if (!country) return Error.NotFound("Country.NotFound","Country not found");
        
        var exists = await context.StatutoryProfiles.AnyAsync(c => c.CountryId == request.CountryId
                                                                   && c.Name == request.Name);
        
        if (exists) return Error.Validation("Profile.Exists", "Statutory profile exists");
        
        var profile = mapper.Map<StatutoryProfile>(request); 
        await context.StatutoryProfiles.AddAsync(profile);
        await context.SaveChangesAsync();
        
        return profile.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<StatutoryProfileDto>>>> GetStatutoryProfiles(int page, int pageSize,
        string searchQuery, Guid? countryId = null)
    {
        var query = context.StatutoryProfiles.AsQueryable();
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Code);
        }
        
        if (countryId.HasValue) query = query.Where(c => c.CountryId == countryId.Value);
        
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,
            mapper.Map<StatutoryProfileDto>);
    }

    public async Task<Result<StatutoryProfileDto>> GetStatutoryProfile(Guid id)
    {
        var profile = await context.StatutoryProfiles.FirstOrDefaultAsync(c => c.Id == id);

        return profile is null ? Error.NotFound("Profile.NotFound", "Statutory profile not found")
            : mapper.Map<StatutoryProfileDto>(profile);
    }

    public async Task<Result> UpdateStatutoryProfile(Guid id, CreateStatutoryProfileRequest request)
    {
        var profile = await context.StatutoryProfiles.FirstOrDefaultAsync(c => c.Id == id);
        
        mapper.Map(request, profile);
        context.StatutoryProfiles.Update(profile);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteStatutoryProfile(Guid id, Guid userId)
    {
        var profile = await context.StatutoryProfiles.FirstOrDefaultAsync(s => s.Id == id);
        if (profile is null)
            return Error.NotFound("Profile.NotFound", "Statutory profile not found");
        
        profile.LastDeletedById = userId;
        profile.DeletedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        return Result.Success();
    }
}