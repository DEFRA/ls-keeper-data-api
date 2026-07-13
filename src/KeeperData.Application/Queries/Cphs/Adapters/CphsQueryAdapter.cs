using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Cphs.Adapters;

public class CphsQueryAdapter(ICphRepository repository)
{
    private readonly ICphRepository _repository = repository;

    public async Task<(List<CphDto> Items, int TotalCount, string? NextCursor)> GetCphsAsync(
        GetCphsQuery query,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            query.Page, query.PageSize, query.Sort, cancellationToken);

        return (items, totalCount, null);
    }
}