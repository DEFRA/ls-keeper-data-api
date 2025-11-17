using KeeperData.Core.Documents;
using MongoDB.Driver;

namespace KeeperData.Application.Queries.Sites.Builders;

public static class SiteFilterBuilder
{
    public static FilterDefinition<SiteDocument> Build(GetSitesQuery query)
    {
        var filters = new List<FilterDefinition<SiteDocument>>();
        var builder = Builders<SiteDocument>.Filter;

        if (query.LastUpdatedDate.HasValue)
        {
            filters.Add(builder.Gte(x => x.LastUpdatedDate, query.LastUpdatedDate.Value));
        }

        if (!string.IsNullOrEmpty(query.SiteIdentifier))
        {
            filters.Add(builder.ElemMatch(x => x.Identifiers, identifierDoc => identifierDoc.Identifier == query.SiteIdentifier));
        }

        if (query.Type is { Count: > 0 })
        {
            filters.Add(builder.In(x => x.Type, query.Type));
        }

        if (query.SiteId.HasValue)
        {
            filters.Add(builder.Eq(x => x.Id, query.SiteId.Value.ToString()));
        }

        // TODO - Update to use site parties
        //if (query.KeeperPartyId.HasValue)
        //{
        //    filters.Add(builder.AnyEq(x => x.KeeperPartyIds, query.KeeperPartyId.Value.ToString()));
        //}

        return filters.Count > 0 ? builder.And(filters) : builder.Empty;
    }
}