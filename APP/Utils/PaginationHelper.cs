using Microsoft.EntityFrameworkCore;

namespace APP.Utils;

public static class PaginationHelper
{
    public static async Task<Paginateable<IEnumerable<TDto>>> GetPaginatedResultAsync<TEntity, TDto>(
        IQueryable<TEntity> query,
        int page,
        int pageSize,
        Func<TEntity, TDto> mapFunc)
        where TEntity : class
    {
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dto = entities.Select(mapFunc);

        // Calculate start and stop page indices
        var halfPagesToShow = pageSize / 2;
        var startPageIndex = Math.Max(1, page - halfPagesToShow);
        var stopPageIndex = Math.Min(totalPages, page + halfPagesToShow);

        // Ensure that we always show the correct number of pages if possible
        if (stopPageIndex - startPageIndex + 1 < pageSize)
        {
            if (startPageIndex == 1)
            {
                stopPageIndex = Math.Min(totalPages, startPageIndex + pageSize - 1);
            }
            else if (stopPageIndex == totalPages)
            {
                startPageIndex = Math.Max(1, stopPageIndex - pageSize + 1);
            }
        }

        return new Paginateable<IEnumerable<TDto>>
        {
            Data = dto,
            PageIndex = page,
            PageCount = totalPages,
            TotalRecordCount = totalCount,
            NumberOfPagesToShow = pageSize,
            StartPageIndex = startPageIndex,
            StopPageIndex = stopPageIndex
        };
    }
    
    /*public static async Task<Paginateable<TDto>> GetPaginatedsResultAsync<TEntity, TDto>(
        IQueryable<TEntity> query,
        int pageNumber,
        int pageSize,
        Func<TEntity, TDto> mapFunc)
        where TEntity : class
    {
        pageNumber = pageNumber == 0 ? 1 : pageNumber;
        pageSize = pageSize == 0 ? 10 : pageSize;
        int count = await query.CountAsync();
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;

        List<TEntity> items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        var dto = items.Select(mapFunc).ToList();

        return new Paginateables<TDto>(dto, count, pageNumber, pageSize);
    }*/
}
