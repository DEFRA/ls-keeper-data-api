using KeeperData.Application.Queries.Parties.Builders;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Parties.Adapters;

public class PartiesQueryAdapter(IPartiesRepository repository)
{
    private readonly IPartiesRepository _repository = repository;

    public async Task<(List<PartyDocument> Items, int TotalCount)> GetPartiesAsync(
        GetPartiesQuery query,
        CancellationToken cancellationToken = default)
    {
        /* TODO untested */
        var filterDefinition = PartyFilterBuilder.Build(query);
        var sortDefinition = PartySortBuilder.Build(query);

        var totalCount = await _repository.CountAsync(filterDefinition, cancellationToken);

        var items = await _repository.FindAsync(
            filter: filterDefinition,
            sort: sortDefinition,
            skip: (query.Page - 1) * query.PageSize,
            take: query.PageSize,
            cancellationToken: cancellationToken);

        return (items, totalCount);
        
    }
}