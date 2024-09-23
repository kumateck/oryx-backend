namespace APP.Utils;

public class Paginateable<T>
{
    public T Data { get; set; }
    public int PageIndex { get; set; }
    public int PageCount { get; set; }
    public int TotalRecordCount { get; set; }
    public int NumberOfPagesToShow { get; set; }
    public int StartPageIndex { get; set; }
    public int StopPageIndex { get; set; }
}

// public class Paginateables<T>(List<T> data = default, int count = 0, int page = 1, int pageSize = 10)
// {
//     public List<T> Data { get; set; } = data;
//     public int CurrentPage { get; set; } = page;
//     public int TotalPages { get; set; } = (int)Math.Ceiling(count / (double)pageSize);
//     public int TotalCount { get; set; } = count;
//     public int PageSize { get; set; } = pageSize;
//     public bool HasPreviousPage => CurrentPage > 1;
//     public bool HasNextPage => CurrentPage < TotalPages;
// }