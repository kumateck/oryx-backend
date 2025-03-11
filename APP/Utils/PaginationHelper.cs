using Microsoft.EntityFrameworkCore;
using SHARED;

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
        
        query = ApplySorting(query, "CreatedAt", SortDirection.Descending);

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
    
    public static async Task<Paginateable<IQueryable<TEntity>>> GetPaginatedResultAsync<TEntity>(
        IQueryable<TEntity> query,
        int page,
        int pageSize)
        where TEntity : class
    {
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var entities =  query
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
        
        var halfPagesToShow = pageSize / 2;
        var startPageIndex = Math.Max(1, page - halfPagesToShow);
        var stopPageIndex = Math.Min(totalPages, page + halfPagesToShow);

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

        return new Paginateable<IQueryable<TEntity>>
        {
            Data = entities,
            PageIndex = page,
            PageCount = totalPages,
            TotalRecordCount = totalCount,
            StartPageIndex = startPageIndex,
            StopPageIndex = stopPageIndex
        };
    }
    
    public static Paginateable<IEnumerable<T>> Paginate<T>(int page, int pageSize, List<T> source)
    {
        var totalCount = source.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var paginatedData = source
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

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

        return new Paginateable<IEnumerable<T>>
        {
            Data = paginatedData,
            PageIndex = page,
            PageCount = totalPages,
            TotalRecordCount = totalCount,
            StartPageIndex = startPageIndex,
            StopPageIndex = stopPageIndex
        };
    }
    
   public static async Task<Paginateable<IEnumerable<TDto>>> GetPaginatedResultAsync<TEntity, TDto>(
        IQueryable<TEntity> query,
        PagedQuery pagedQuery,
        Func<TEntity, TDto> mapFunc)
        where TEntity : class
    {
        if (!string.IsNullOrEmpty(pagedQuery.SortLabel))
        {
            query = ApplySorting(query, pagedQuery.SortLabel, pagedQuery.SortDirection);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pagedQuery.PageSize);

        // Apply pagination
        var entities = await query
            .Skip((pagedQuery.Page - 1) * pagedQuery.PageSize)
            .Take(pagedQuery.PageSize)
            .ToListAsync();

        var dto = entities.Select(mapFunc);

        // Calculate start and stop page indices
        var halfPagesToShow = pagedQuery.PageSize / 2;
        var startPageIndex = Math.Max(1, pagedQuery.Page - halfPagesToShow);
        var stopPageIndex = Math.Min(totalPages, pagedQuery.Page + halfPagesToShow);

        if (stopPageIndex - startPageIndex + 1 < pagedQuery.PageSize)
        {
            if (startPageIndex == 1)
            {
                stopPageIndex = Math.Min(totalPages, startPageIndex + pagedQuery.PageSize - 1);
            }
            else if (stopPageIndex == totalPages)
            {
                startPageIndex = Math.Max(1, stopPageIndex - pagedQuery.PageSize + 1);
            }
        }

        return new Paginateable<IEnumerable<TDto>>
        {
            Data = dto,
            PageIndex = pagedQuery.Page,
            PageCount = totalPages,
            TotalRecordCount = totalCount,
            NumberOfPagesToShow = pagedQuery.PageSize,
            StartPageIndex = startPageIndex,
            StopPageIndex = stopPageIndex
        };
    }

    // Optional: Apply sorting logic if needed
    private static IQueryable<TEntity> ApplySorting<TEntity>(IQueryable<TEntity> query, string sortLabel, SortDirection sortDirection)
    {
        query = sortDirection switch
        {
            SortDirection.Ascending => query.OrderBy(e => EF.Property<object>(e, sortLabel)),
            SortDirection.Descending => query.OrderByDescending(e => EF.Property<object>(e, sortLabel)),
            _ => query
        };
        return query;
    }
}
