using KeeperData.Core.Documents;
using MongoDB.Driver;

namespace KeeperData.Application.Queries.Sites.Builders;

public static class SiteSortBuilder
{
    public static SortDefinition<SiteDocument> Build(GetSitesQuery query)
    {
        var sortBuilder = Builders<SiteDocument>.Sort;
        var sortField = query.Order?.ToLowerInvariant() ?? "name";
        var sortDirection = query.Sort?.ToLowerInvariant() ?? "asc";

        var sortFieldPath = GetSortFieldPath(sortField);
        var primarySort = sortDirection == "desc"
            ? sortBuilder.Descending(sortFieldPath)
            : sortBuilder.Ascending(sortFieldPath);

        if (sortFieldPath == "_id") return primarySort;

        return sortDirection == "desc"
            ? primarySort.Descending(x => x.Id)
            : primarySort.Ascending(x => x.Id);
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