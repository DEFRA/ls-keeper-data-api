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
        var (filterDefinition, hasValidCursor) = CursorPaginationHelper.ApplyCursorFilter(
            SiteFilterBuilder.Build(query), query.Cursor, query.Sort, SiteSortBuilder.GetSortFieldPath(query.Order));

        var sortDefinition = SiteSortBuilder.Build(query);
        var totalCount = await _repository.CountAsync(filterDefinition, cancellationToken);

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

    private static string GetSortValue(SiteDocument doc, string? sortField)
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