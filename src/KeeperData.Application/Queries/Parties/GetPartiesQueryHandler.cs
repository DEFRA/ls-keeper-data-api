using KeeperData.Application.Queries.Pagination;
using KeeperData.Application.Queries.Parties.Adapters;
using KeeperData.Core.DTOs;
using MediatR;

namespace KeeperData.Application.Queries.Parties;

public class GetPartiesQueryHandler(PartiesQueryAdapter adapter)
    : IRequestHandler<GetPartiesQuery, PaginatedResult<PartyDto>>
{
    public async Task<PaginatedResult<PartyDto>> Handle(GetPartiesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount, nextCursor) = await adapter.GetPartiesAsync(request, cancellationToken);

        return new PaginatedResult<PartyDto>
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