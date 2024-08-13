using APP.Extensions;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Sites;
using INFRASTRUCTURE.Context;
using SHARED;

namespace APP.Repository;

public class SiteRepository(IMapper mapper, ApplicationDbContext context)
{
    public async Task<Result<Guid>> CreateSite(CreateSiteRequest request, Guid userId)
    {

        if (string.IsNullOrEmpty(request.Name))
            return SiteErrors.InvalidName(request.Name);
        
        var site = mapper.Map<Site>(request);
        site.CreatedById = userId;
        await context.Sites.AddAsync(site);
        await context.SaveChangesAsync();

        return site.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<SiteDto>>>> GetSites(int page, int pageSize, string searchQuery)
    {
        var query = context.Sites.AsQueryable();
        
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Name, q => q.Description);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query, 
            page, 
            pageSize, 
            mapper.Map<SiteDto>
        );
    }
}