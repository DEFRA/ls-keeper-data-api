using MediatR;

namespace KeeperData.Application.Queries.Pagination;

public abstract class PagedQueryHandler<TQuery, TDocument>
    : IRequestHandler<TQuery, PaginatedResult<TDocument>>
    where TQuery : IPagedQuery<TDocument>
{
    protected abstract Task<(List<TDocument> Items, int TotalCount, string? NextCursor)> FetchAsync(TQuery request, CancellationToken cancellationToken);

    public async Task<PaginatedResult<TDocument>> Handle(TQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount, nextCursor) = await FetchAsync(query, cancellationToken);

        return new PaginatedResult<TDocument>
        {
            Count = items.Count,
            TotalCount = totalCount,
            Values = items,
            Page = query.Page,
            PageSize = query.PageSize,
            NextCursor = nextCursor
        };
    }
}