using MediatR;

namespace KeeperData.Application.Queries.Pagination;

public abstract class PagedQueryHandler<TQuery, TDocument>
    : IRequestHandler<TQuery, PaginatedResult<TDocument>>
    where TQuery : IPagedQuery<TDocument>
{
    protected abstract Task<(List<TDocument> Items, int TotalCount)> FetchAsync(TQuery request, CancellationToken cancellationToken);

    public async Task<PaginatedResult<TDocument>> Handle(TQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await FetchAsync(query, cancellationToken);

        return new PaginatedResult<TDocument>
        {
            Count = totalCount,
            Values = items,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}