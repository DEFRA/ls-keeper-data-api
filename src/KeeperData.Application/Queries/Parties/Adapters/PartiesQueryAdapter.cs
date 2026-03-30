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
        var (filterDefinition, hasValidCursor) = CursorPaginationHelper.ApplyCursorFilter(
            PartyFilterBuilder.Build(query), query.Cursor, query.Sort, PartySortBuilder.GetSortFieldPath(query.Order));

        var sortDefinition = PartySortBuilder.Build(query);
        var totalCount = await _repository.CountAsync(filterDefinition, cancellationToken);

        // fallback to skip for backward compatibility
        var skip = !hasValidCursor ? (query.Page - 1) * query.PageSize : 0;

        var items = await _repository.FindAsync(
            filter: filterDefinition,
            sort: sortDefinition,
            skip: skip,
            take: query.PageSize,
            cancellationToken: cancellationToken);

        var nextCursor = CursorPaginationHelper.GetNextCursor(items, query.PageSize, doc => GetSortValue(doc, query.Order));

        return (items ?? [], totalCount, nextCursor);
    }

    private string GetSortValue(PartyDocument doc, string? sortField)
    {
        return (sortField?.ToLowerInvariant()) switch
        {
            "id" => doc.Id,
            "name" => doc.Name ?? string.Empty,
            _ => doc.Name ?? string.Empty
        };
    }
}