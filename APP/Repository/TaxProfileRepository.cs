using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.TaxProfiles;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class TaxProfileRepository(ApplicationDbContext context, IMapper mapper) : ITaxProfileRepository
{
    public async Task<Result<Guid>> CreateTaxProfile(CreateTaxProfileRequest request)
    {
        var exists = await context.TaxProfiles.AnyAsync(t => t.Code == request.Code
        && t.CountryId == request.CountryId);
        
        if (exists) return Error.Validation("TaxProfile.Exists", "Tax profile exists");
        
        var taxProfile = mapper.Map<CreateTaxProfileRequest, TaxProfile>(request);
        await context.TaxProfiles.AddAsync(taxProfile);
        await context.SaveChangesAsync();
        
        return taxProfile.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<TaxProfileDto>>>> GetTaxProfiles(int page, int pageSize,
        string searchQuery, Guid? countryId = null)
    {
        var query = context.TaxProfiles.AsQueryable();
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q=> q.Name);
        }

        if (countryId.HasValue)
        {
            query = query.Where(t => t.CountryId == countryId.Value);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,
            mapper.Map<TaxProfileDto>);
    }

    public async Task<Result<TaxProfileDto>> GetTaxProfile(Guid id)
    {
        var tax = await context.TaxProfiles.FirstOrDefaultAsync(t => t.Id == id);
        return tax is null ? Error.NotFound("TaxProfile.NotFound", "Tax profile not found")
            : mapper.Map<TaxProfileDto>(tax);
    }

    public async Task<Result> UpdateTaxProfile(Guid id, CreateTaxProfileRequest request)
    {
        var tax = await context.TaxProfiles.FirstOrDefaultAsync(t => t.Id == id);
        if (tax is null) return Error.NotFound("TaxProfile.NotFound", "Tax profile not found");
        
        mapper.Map(request, tax);
        context.TaxProfiles.Update(tax);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteTaxProfile(Guid id, Guid userId)
    {
        var tax = await context.TaxProfiles.FirstOrDefaultAsync(t => t.Id == id);
        if (tax is null) return Error.NotFound("TaxProfile.NotFound", "Tax profile not found");
        
        tax.DeletedAt = DateTime.UtcNow;
        tax.LastDeletedById = userId;
        
        await context.SaveChangesAsync();
        return Result.Success();
    }
}