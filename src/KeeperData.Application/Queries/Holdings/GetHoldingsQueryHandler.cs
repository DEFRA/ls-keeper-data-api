using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Holdings;

public class GetHoldingsQueryHandler(IHoldingDetailRepository repository)
    : PagedQueryHandler<GetHoldingsQuery, HoldingDetail>
{
    private readonly IHoldingDetailRepository _repository = repository;

    protected override async Task<(List<HoldingDetail> Items, int TotalCount, string? NextCursor)> FetchAsync(
        GetHoldingsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedHoldingsAsync(
            request.Page,
            request.PageSize,
            request.Sort,
            request.Order,
            cancellationToken);

        return (items, totalCount, null);
    }
}