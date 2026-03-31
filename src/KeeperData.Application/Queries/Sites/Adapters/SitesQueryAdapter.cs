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
        return await CursorPaginationHelper.ExecutePagedQueryAsync(
            query,
            SiteFilterBuilder.Build(query),
            SiteSortBuilder.Build(query),
            SiteSortBuilder.GetSortFieldPath(query.Order),
            _repository.CountAsync,
            _repository.FindAsync,
            doc => GetSortValue(doc, query.Order),
            cancellationToken);
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