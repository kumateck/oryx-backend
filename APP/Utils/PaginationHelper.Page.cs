namespace APP.Utils;

public static partial class PaginationHelper
{
    /// <summary>A page built from rows already read and the total the query counted, with the usual page-window fields.</summary>
    public static Paginateable<IEnumerable<T>> Page<T>(IReadOnlyList<T> rows, int page, int pageSize, int totalCount)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)Math.Max(1, pageSize));
        var half = pageSize / 2;
        var start = Math.Max(1, page - half);
        var stop = Math.Min(totalPages, page + half);
        return new Paginateable<IEnumerable<T>>
        {
            Data = rows, PageIndex = page, PageCount = totalPages, TotalRecordCount = totalCount,
            NumberOfPagesToShow = pageSize, StartPageIndex = start, StopPageIndex = stop
        };
    }
}
