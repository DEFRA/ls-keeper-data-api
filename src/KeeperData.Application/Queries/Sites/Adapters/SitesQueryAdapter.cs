using KeeperData.Application.Queries.Pagination;
using KeeperData.Application.Queries.Sites.Builders;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using MongoDB.Driver;

namespace KeeperData.Application.Queries.Sites.Adapters;

public class SitesQueryAdapter(ISitesRepository repository)
{
    private readonly ISitesRepository _repository = repository;

    public async Task<(List<SiteDocument> Items, int TotalCount, string? NextCursor)> GetSitesAsync(
        GetSitesQuery query,
        CancellationToken cancellationToken = default)
    {
        var filterDefinition = SiteFilterBuilder.Build(query);

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            var cursorFilter = BuildCursorFilter(query);
            if (cursorFilter != null)
            {
                filterDefinition = Builders<SiteDocument>.Filter.And(filterDefinition, cursorFilter);
            }
        }

        var sortDefinition = SiteSortBuilder.Build(query);
        var totalCount = await _repository.CountAsync(filterDefinition, cancellationToken);

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

    private FilterDefinition<SiteDocument>? BuildCursorFilter(GetSitesQuery query)
    {
        var decoded = CursorHelper.Decode(query.Cursor);
        if (decoded == null) return null;

        var (sortVal, lastId) = decoded.Value;
        var sortFieldPath = SiteSortBuilder.GetSortFieldPath(query.Order);
        var isAscending = (query.Sort?.ToLowerInvariant() ?? "asc") == "asc";

        var builder = Builders<SiteDocument>.Filter;

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

    private string GetSortValue(SiteDocument doc, string? sortField)
    {
        return (sortField?.ToLowerInvariant()) switch
        {
            "name" => doc.Name ?? string.Empty,
            "type" => doc.Type?.Code ?? string.Empty,
            "siteidentifier" => doc.Identifiers?.FirstOrDefault()?.Identifier ?? string.Empty,
            _ => doc.Name ?? string.Empty
        };
    }
}