using KeeperData.Application.Queries.Cphs.Adapters;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.Cphs;

public class GetCphsQueryHandler(CphsQueryAdapter adapter)
    : PagedQueryHandler<GetCphsQuery, CphDto>
{
    private readonly CphsQueryAdapter _adapter = adapter;

    protected override async Task<(List<CphDto> Items, int TotalCount, string? NextCursor)> FetchAsync(GetCphsQuery request, CancellationToken cancellationToken)
    {
        return await _adapter.GetCphsAsync(request, cancellationToken);
    }
}