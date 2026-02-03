using KeeperData.Core.Documents;
using MongoDB.Driver;

namespace KeeperData.Application.Queries.Parties.Builders;

public static class PartyFilterBuilder
{
    public static FilterDefinition<PartyDocument> Build(GetPartiesQuery query)
    {
        var filters = new List<FilterDefinition<PartyDocument>>();
        var builder = Builders<PartyDocument>.Filter;
        filters.Add(builder.Eq(x => x.Deleted, false));

        if (query.LastUpdatedDate.HasValue)
        {
            filters.Add(builder.Gte(x => x.LastUpdatedDate, query.LastUpdatedDate.Value));
        }

        if (query.FirstName != null)
        {
            filters.Add(builder.Eq(x => x.FirstName, query.FirstName));
        }

        if (query.LastName != null)
        {
            filters.Add(builder.Eq(x => x.LastName, query.LastName));
        }

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            filters.Add(builder.ElemMatch(x => x.Communication, c => c.Email == query.Email));
        }

        return builder.And(filters);
    }
}