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
        return await CursorPaginationHelper.ExecutePagedQueryAsync(
            query,
            PartyFilterBuilder.Build(query),
            PartySortBuilder.Build(query),
            PartySortBuilder.GetSortFieldPath(query.Order),
            _repository.CountAsync,
            _repository.FindAsync,
            doc => GetSortValue(doc, query.Order),
            cancellationToken);
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