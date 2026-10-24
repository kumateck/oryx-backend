using APP.Utils;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Materials;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Grns;

public sealed class GrnListReader(ApplicationDbContext context)
{
    public async Task<Result<Paginateable<IEnumerable<GrnSummaryDto>>>> Read(
        int page, int pageSize, string search, MaterialKind? kind, Status? status)
    {
        if (page < 1 || pageSize < 1 || (long)(page - 1) * pageSize > int.MaxValue)
            return Error.Validation("Grn.Pagination", "Page and page size must be positive and within the supported range.");
        var query = GrnListQueries.Filter(context, search, kind, status);
        // The helper runs CountAsync only; materialization happens once in the scalar page query below.
        var pagination = await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize);
        var rows = await GrnListQueries.Page(query, page, pageSize).ToListAsync();
        if (rows.Count > 0)
        {
            var ids = rows.Select(row => row.Id).ToArray();
            var batches = (await GrnListQueries.Batches(context, ids).ToListAsync())
                .ToLookup(row => row.GrnId, row => row.Batch);
            foreach (var row in rows) row.MaterialBatches = batches[row.Id].ToList();
        }
        return Result.Success(new Paginateable<IEnumerable<GrnSummaryDto>> {
            Data = rows, PageIndex = pagination.PageIndex, PageCount = pagination.PageCount,
            TotalRecordCount = pagination.TotalRecordCount, NumberOfPagesToShow = pagination.NumberOfPagesToShow,
            StartPageIndex = pagination.StartPageIndex, StopPageIndex = pagination.StopPageIndex
        });
    }
}
