using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Documents;
using MongoDB.Driver;

namespace KeeperData.Application.Queries.Sites.Builders;

public static class SiteSortBuilder
{
    public static SortDefinition<SiteDocument> Build(GetSitesQuery query)
    {
        var sortFieldPath = GetSortFieldPath(query.Order);
        return CursorPaginationHelper.BuildSortDefinition<SiteDocument>(query.Sort, sortFieldPath);
    }

    public static string GetSortFieldPath(string? field)
    {
        return (field?.ToLowerInvariant()) switch
        {
            "name" => "name",
            "type" => "type.code",
            "siteidentifier" => "identifiers.identifier",
            _ => "name"
        };
    }
}