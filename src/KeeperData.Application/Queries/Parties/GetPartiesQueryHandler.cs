using KeeperData.Application.Queries.Pagination;
using KeeperData.Application.Queries.Parties.Adapters;
using KeeperData.Core.Documents;

namespace KeeperData.Application.Queries.Parties;

public class GetPartiesQueryHandler(PartiesQueryAdapter adapter)
    : PagedQueryHandler<GetPartiesQuery, PartyDocument>
{
    private readonly PartiesQueryAdapter _adapter = adapter;

    protected override async Task<(List<PartyDocument> Items, int TotalCount)> FetchAsync(GetPartiesQuery query, CancellationToken cancellationToken)
    {
        return await _adapter.GetPartiesAsync(query, cancellationToken);
    }
}