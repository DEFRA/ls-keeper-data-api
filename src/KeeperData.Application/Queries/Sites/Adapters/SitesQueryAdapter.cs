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
        bool hasValidCursor = false;

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            var cursorFilter = CursorPaginationHelper.BuildCursorFilter<SiteDocument>(query.Cursor, query.Sort, SiteSortBuilder.GetSortFieldPath(query.Order));
            if (cursorFilter != null)
            {
                filterDefinition = Builders<SiteDocument>.Filter.And(filterDefinition, cursorFilter);
                hasValidCursor = true;
            }
        }

        var sortDefinition = SiteSortBuilder.Build(query);
        var totalCount = await _repository.CountAsync(filterDefinition, cancellationToken);

        var skip = !hasValidCursor ? (query.Page - 1) * query.PageSize : 0;

        var items = await _repository.FindAsync(
            filter: filterDefinition,
            sort: sortDefinition,
            skip: skip,
            take: query.PageSize,
            cancellationToken: cancellationToken);

        string? nextCursor = null;
        if (items != null && items.Count == query.PageSize)
        {
            var lastItem = items.Last();
            var sortVal = GetSortValue(lastItem, query.Order);
            nextCursor = CursorHelper.Encode(sortVal, lastItem.Id);
        }

        return (items ?? [], totalCount, nextCursor);
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