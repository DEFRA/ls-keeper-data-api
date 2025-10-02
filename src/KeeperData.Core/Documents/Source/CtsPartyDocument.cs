using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KeeperData.Core.Documents.Source;

[CollectionName("ctsParties")]
public class CtsPartyDocument : IEntity, IContainsIndexes
{
    public string Id { get; set; } = string.Empty;
    public string PartyId { get; set; } = string.Empty;

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("PartyId"),
                new CreateIndexOptions { Name = "idx_partyId" })
        ];
    }
}