using KeeperData.Core.Documents;
using MongoDB.Driver;

namespace KeeperData.Application.Queries.Parties.Builders;

public static class PartySortBuilder
{
    public static SortDefinition<PartyDocument> Build(GetPartiesQuery query)
    {
        var sortBuilder = Builders<PartyDocument>.Sort;

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
            "id" => "customerNumber",
            "name" => "name",
            _ => "name"
        };
    }
}