namespace KeeperData.Application.Queries.Pagination;

public class PaginatedResult<T>
{
    public int Count { get; set; }  // Number of items on current page
    public int TotalCount { get; set; }  // Total number of items across all pages
    public List<T> Values { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}