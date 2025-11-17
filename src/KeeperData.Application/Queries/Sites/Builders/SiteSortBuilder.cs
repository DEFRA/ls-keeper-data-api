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

        // can't use a strongly-typed expression for nested array fields
        var sortFieldPath = GetSortFieldPath(sortField);

        return sortDirection switch
        {
            "desc" => sortBuilder.Descending(sortFieldPath),
            _ => sortBuilder.Ascending(sortFieldPath)
        };
    }

    private static string GetSortFieldPath(string field)
    {
        return field switch
        {
            "name" => "name",
            "type" => "type",
            "state" => "state",
            "identifier" => "identifiers.identifier", // replaces PrimaryIdentifier
            _ => "type"
        };
    }
}