using KeeperData.Core.Documents;
using MongoDB.Driver;

namespace KeeperData.Application.Queries.Sites.Builders;

public static class SiteFilterBuilder
{
    public static FilterDefinition<SiteDocument> Build(GetSitesQuery query)
    {
        var filters = new List<FilterDefinition<SiteDocument>>();
        var builder = Builders<SiteDocument>.Filter;
        filters.Add(builder.Eq(x => x.Deleted, false));

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
            filters.Add(builder.And(
                builder.Ne(x => x.Type, null),
                builder.In("type.code", query.Type)
            ));
        }

        if (query.SiteId.HasValue)
        {
            filters.Add(builder.Eq(x => x.Id, query.SiteId.Value.ToString()));
        }

        if (query.KeeperPartyId.HasValue)
        {
            var partyIdString = query.KeeperPartyId.Value.ToString();

            filters.Add(builder.ElemMatch(x => x.Parties, p => p.CustomerNumber == partyIdString));
        }

        return builder.And(filters);
    }
}