using KeeperData.Application.Queries.Pagination;
using KeeperData.Application.Queries.Sites.Adapters;
using KeeperData.Core.DTOs;
using MediatR;

namespace KeeperData.Application.Queries.Sites;

public class GetSitesQueryHandler(SitesQueryAdapter adapter)
    : IRequestHandler<GetSitesQuery, PaginatedResult<SiteDto>>
{
    public async Task<PaginatedResult<SiteDto>> Handle(GetSitesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount, nextCursor) = await adapter.GetSitesAsync(request, cancellationToken);

        return new PaginatedResult<SiteDto>
        {
            Count = items.Count,
            TotalCount = totalCount,
            Values = items,
            Page = request.Page,
            PageSize = request.PageSize,
            NextCursor = nextCursor
        };
    }
}