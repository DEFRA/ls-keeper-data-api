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
        var filterDefinition = PartyFilterBuilder.Build(query);

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            var cursorFilter = BuildCursorFilter(query);
            if (cursorFilter != null)
            {
                filterDefinition = Builders<PartyDocument>.Filter.And(filterDefinition, cursorFilter);
            }
        }

        var sortDefinition = PartySortBuilder.Build(query);
        var totalCount = await _repository.CountAsync(filterDefinition, cancellationToken);

        // Fallback to skip for backward compatibility
        var skip = string.IsNullOrWhiteSpace(query.Cursor) ? (query.Page - 1) * query.PageSize : 0;

        var items = await _repository.FindAsync(
            filter: filterDefinition,
            sort: sortDefinition,
            skip: skip,
            take: query.PageSize,
            cancellationToken: cancellationToken);

        string? nextCursor = null;
        if (items.Count == query.PageSize)
        {
            var lastItem = items.Last();
            var sortVal = GetSortValue(lastItem, query.Order);
            nextCursor = CursorHelper.Encode(sortVal, lastItem.Id);
        }

        return (items, totalCount, nextCursor);
    }

    private FilterDefinition<PartyDocument>? BuildCursorFilter(GetPartiesQuery query)
    {
        var decoded = CursorHelper.Decode(query.Cursor);
        if (decoded == null) return null;

        var (sortVal, lastId) = decoded.Value;
        var sortFieldPath = PartySortBuilder.GetSortFieldPath(query.Order);
        var isAscending = (query.Sort?.ToLowerInvariant() ?? "asc") == "asc";

        var builder = Builders<PartyDocument>.Filter;

        if (isAscending)
        {
            return builder.Or(
                builder.Gt(sortFieldPath, sortVal),
                builder.And(builder.Eq(sortFieldPath, sortVal), builder.Gt(x => x.Id, lastId))
            );
        }
        else
        {
            return builder.Or(
                builder.Lt(sortFieldPath, sortVal),
                builder.And(builder.Eq(sortFieldPath, sortVal), builder.Lt(x => x.Id, lastId))
            );
        }
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