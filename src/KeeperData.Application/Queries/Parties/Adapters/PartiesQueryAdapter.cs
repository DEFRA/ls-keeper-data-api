using KeeperData.Application.Queries.Pagination;
using KeeperData.Application.Queries.Parties.Builders;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using MongoDB.Driver;

namespace KeeperData.Application.Queries.Parties.Adapters;

public class PartiesQueryAdapter(IPartiesRepository repository)
{
    private readonly IPartiesRepository _repository = repository;

    public async Task<(List<PartyDocument> Items, int TotalCount, string? NextCursor)> GetPartiesAsync(
        GetPartiesQuery query,
        CancellationToken cancellationToken = default)
    {
        var options = new CursorPaginationHelper.PagedQueryOptions<PartyDocument, GetPartiesQuery>
        {
            Query = query,
            BaseFilter = PartyFilterBuilder.Build(query),
            SortDefinition = PartySortBuilder.Build(query),
            SortFieldPath = PartySortBuilder.GetSortFieldPath(query.Order),
            CountAsync = _repository.CountAsync,
            FindAsync = _repository.FindAsync,
            GetSortValue = doc => GetSortValue(doc, query.Order)
        };

        return await CursorPaginationHelper.ExecutePagedQueryAsync(options, cancellationToken);
    }

    private static string GetSortValue(PartyDocument doc, string? sortField)
    {
        return (sortField?.ToLowerInvariant()) switch
        {
            "id" => doc.Id,
            "name" => doc.Name ?? string.Empty,
            _ => doc.Name ?? string.Empty
        };
    }
}