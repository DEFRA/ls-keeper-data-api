using KeeperData.Application.Queries.Pagination;
using KeeperData.Application.Queries.Sites.Builders;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Sites.Adapters;

public class SitesQueryAdapter(ISitesRepository repository)
{
    public async Task<(List<SiteDto> Items, int TotalCount, string? NextCursor)> GetSitesAsync(
    GetSitesQuery query,
    CancellationToken cancellationToken = default)
    {
        var options = new CursorPaginationHelper.PagedQueryOptions<SiteDocument, SitesQueryInternal>
        {
            Query = new SitesQueryInternal(query),
            BaseFilter = SiteFilterBuilder.Build(query),
            SortDefinition = SiteSortBuilder.Build(query),
            SortFieldPath = SiteSortBuilder.GetSortFieldPath(query.Order),
            CountAsync = repository.CountAsync,
            FindAsync = repository.FindAsync,
            GetSortValue = doc => GetSortValue(doc, query.Order)
        };

        var (documents, totalCount, nextCursor) = await CursorPaginationHelper.ExecutePagedQueryAsync(options, cancellationToken);

        var dtos = documents.Select(doc => doc.ToDto()).ToList();

        return (dtos, totalCount, nextCursor);
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

    /// <summary>
    /// Internal wrapper that satisfies IPagedQuery&lt;SiteDocument&gt; for the CursorPaginationHelper,
    /// while the public GetSitesQuery uses IPagedQuery&lt;SiteDto&gt;.
    /// </summary>
    private class SitesQueryInternal(GetSitesQuery query) : IPagedQuery<SiteDocument>
    {
        public int Page => query.Page;
        public int PageSize => query.PageSize;
        public string? Order => query.Order;
        public string? Sort => query.Sort;
        public string? Cursor => query.Cursor;
    }
}