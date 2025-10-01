using KeeperData.Core.Documents;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace KeeperData.Application.Queries.Sites.Builders;

public static class SiteSortBuilder
{
    public static SortDefinition<SiteDocument> Build(GetSitesQuery query)
    {
        var sortBuilder = Builders<SiteDocument>.Sort;

        var sortField = query.Order?.ToLowerInvariant() ?? "name";
        var sortDirection = query.Sort?.ToLowerInvariant() ?? "asc";

        return sortDirection switch
        {
            "desc" => sortBuilder.Descending(GetSortExpression(sortField)),
            _ => sortBuilder.Ascending(GetSortExpression(sortField))
        };
    }

    private static Expression<Func<SiteDocument, object>> GetSortExpression(string field)
    {
        return field switch
        {
            "name" => x => x.Name,
            "type" => x => x.Type,
            "state" => x => x.State,
            "identifier" => x => x.PrimaryIdentifier,
            _ => x => x.Type
        };
    }
}