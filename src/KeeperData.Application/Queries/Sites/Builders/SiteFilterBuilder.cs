using KeeperData.Core.Documents;
using MongoDB.Driver;

namespace KeeperData.Application.Queries.Sites.Builders;

public static class SiteFilterBuilder
{
    public static FilterDefinition<SiteDocument> Build(GetSitesQuery query)
    {
        var filters = new List<FilterDefinition<SiteDocument>>();

        if (query.LastUpdatedDate.HasValue)
        {
            filters.Add(Builders<SiteDocument>.Filter.Gte(
                x => x.LastUpdatedDate, query.LastUpdatedDate.Value));
        }

        // TODO - Add all filters

        return filters.Count != 0
            ? Builders<SiteDocument>.Filter.And(filters)
            : Builders<SiteDocument>.Filter.Empty;
    }
}
