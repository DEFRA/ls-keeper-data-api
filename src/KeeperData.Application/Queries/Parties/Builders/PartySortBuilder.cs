using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Documents;
using MongoDB.Driver;

namespace KeeperData.Application.Queries.Parties.Builders;

public static class PartySortBuilder
{
    public static SortDefinition<PartyDocument> Build(GetPartiesQuery query)
    {
        var sortFieldPath = GetSortFieldPath(query.Order);
        return CursorPaginationHelper.BuildSortDefinition<PartyDocument>(query.Sort, sortFieldPath);
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