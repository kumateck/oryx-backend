namespace SHARED;

/// <summary>
/// A base object for all paged requests
/// </summary>
public abstract class PagedQuery
{
    /// <summary>
    /// The size of the items on each page
    /// </summary>
    public int PageSize { get; set; } = 5;

    /// <summary>
    /// The page position to get
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// The sort name for sorting
    /// </summary>
    public string SortLabel { get; set; }

    /// <summary>
    /// The direction of the sort
    /// </summary>
    public SortDirection SortDirection { get; set; }
}

public enum SortDirection
{
    None = 0,
    Ascending = 1,
    Descending = 2
}