using KeeperData.Application.Queries.Pagination;
using KeeperData.Application.Queries.Parties;
using KeeperData.Application.Queries.Parties.Builders;
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
        var options = new CursorPaginationHelper.PagedQueryOptions<SiteDocument, GetSitesQuery>
        {
            Query = query,
            BaseFilter = SiteFilterBuilder.Build(query),
            SortDefinition = SiteSortBuilder.Build(query),
            SortFieldPath = SiteSortBuilder.GetSortFieldPath(query.Order),
            CountAsync = _repository.CountAsync,
            FindAsync = _repository.FindAsync,
            GetSortValue = doc => GetSortValue(doc, query.Order)
        };

        return await CursorPaginationHelper.ExecutePagedQueryAsync(options, cancellationToken);
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