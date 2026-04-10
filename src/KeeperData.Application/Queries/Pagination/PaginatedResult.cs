namespace KeeperData.Application.Queries.Pagination;

/// <summary>
/// A paginated result containing a subset of items and pagination metadata.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// Number of items on the current page.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// The items on the current page.
    /// </summary>
    public List<T> Values { get; set; } = [];

    /// <summary>
    /// The current page number (1-based).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// The total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there is a next page available.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Whether there is a previous page available.
    /// </summary>
    public bool HasPreviousPage => Page > 1;
    public string? NextCursor { get; set; }

    /// <summary>
    /// Projects the items in this result to a different type, preserving pagination metadata.
    /// </summary>
    public PaginatedResult<U> Map<U>(Func<T, U> mapper) => new()
    {
        Count = Count,
        TotalCount = TotalCount,
        Values = Values.Select(mapper).ToList(),
        Page = Page,
        PageSize = PageSize,
        NextCursor = NextCursor
    };
}