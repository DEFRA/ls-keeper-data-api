using KeeperData.Application.Queries.Pagination;

namespace KeeperData.Application.Queries;

public interface IPagedQuery<T> : IQuery<PaginatedResult<T>>
{
    int Page { get; }
    int PageSize { get; }

    /// <summary>
    /// Field to sort by
    /// </summary>
    string? Order { get; }

    /// <summary>
    /// "asc" or "desc"
    /// </summary>
    string? Sort { get; }

    /// <summary>
    /// Optional if we add in cursor-based pagination
    /// </summary>
    string? Cursor { get; }
}