using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;
using MediatR;

namespace KeeperData.Application.Queries.Holdings;

public class GetHoldingsQueryHandler(IHoldingDetailRepository repository)
    : IRequestHandler<GetHoldingsQuery, PaginatedResult<HoldingDetail>>
{
    private readonly IHoldingDetailRepository _repository = repository;

    public async Task<PaginatedResult<HoldingDetail>> Handle(
        GetHoldingsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount, dataTimestamp) = string.IsNullOrWhiteSpace(request.Search)
            ? await _repository.GetPagedHoldingsAsync(request.Page, request.PageSize, request.Sort, request.Order, cancellationToken)
            : await _repository.SearchHoldingsAsync(request.Page, request.PageSize, request.Sort, request.Order, request.Search.Trim(), cancellationToken);

        return new PaginatedResult<HoldingDetail>
        {
            Values = items,
            Count = items.Count,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            DataTimestamp = dataTimestamp
        };
    }
}