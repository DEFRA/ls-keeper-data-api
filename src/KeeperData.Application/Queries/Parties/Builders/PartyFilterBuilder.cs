using KeeperData.Core.Documents;
using MongoDB.Driver;

namespace KeeperData.Application.Queries.Parties.Builders;

public static class PartyFilterBuilder
{
    public static FilterDefinition<PartyDocument> Build(GetPartiesQuery query)
    {
        var filters = new List<FilterDefinition<PartyDocument>>();
        var builder = Builders<PartyDocument>.Filter;

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

        /*
        TODO EMAIL! not in dataset
        if (query.Email != null)
        {
            filters.Add(builder.Eq(x => x., query.Email));
        }*/

        return filters.Count > 0 ? builder.And(filters) : builder.Empty;
    }
}