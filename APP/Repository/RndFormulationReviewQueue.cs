using APP.Mapper;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.RndFormulations;
using DOMAIN.Entities.RndProjects;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class RndFormulationReviewQueue
{
    private static IQueryable<RndFormulation> Query(ApplicationDbContext context) =>
        context.RndFormulations.AsNoTracking().AsSplitQuery()
            .Include(item => item.RndProject).ThenInclude(project => project.Product)
            .Include(item => item.RndProject).ThenInclude(project => project.Department)
            .Include(item => item.RndProject).ThenInclude(project => project.RequestedBy)
            .Include(item => item.Items).ThenInclude(item => item.Material)
            .Include(item => item.Items).ThenInclude(item => item.MaterialType)
            .Include(item => item.Items).ThenInclude(item => item.BaseUoM)
            .Include(item => item.Items).ThenInclude(item => item.Substitutes);

    private static RndFormulationReviewDto Map(IMapper mapper, RndFormulation item) => new()
    {
        Formulation = mapper.Map<RndFormulationDto>(item),
        Project = mapper.Map<RndProjectDto>(item.RndProject),
    };

    internal static async Task<Result<Paginateable<IEnumerable<RndFormulationReviewDto>>>> Get(
        ApplicationDbContext context,
        IMapper mapper,
        int page,
        int pageSize
    )
    {
        var query = Query(context)
            .Where(item => item.Status == RndFormulationStatus.InReview)
            .OrderBy(item => item.CreatedAt);

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, item => Map(mapper, item));
    }

    internal static async Task<Result<RndFormulationReviewDto>> GetItem(
        ApplicationDbContext context,
        IMapper mapper,
        Guid formulationId
    )
    {
        var item = await Query(context).FirstOrDefaultAsync(value => value.Id == formulationId);
        return item is null
            ? Error.NotFound("RndFormulation.NotFound", "Formulation not found.")
            : Map(mapper, item);
    }
}
