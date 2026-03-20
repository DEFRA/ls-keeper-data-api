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
            "id" => "_id",
            "name" => "name",
            _ => "name"
        };
    }
}