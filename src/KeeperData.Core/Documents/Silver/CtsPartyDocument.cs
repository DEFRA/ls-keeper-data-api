using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KeeperData.Core.Documents.Silver;

[CollectionName("ctsParties")]
public class CtsPartyDocument : BasePartyDocument, IEntity, IDeletableEntity, IContainsIndexes
{
    public string? Id { get; set; }
    public int LastUpdatedBatchId { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public bool Deleted { get; set; }

    public string CountyParishHoldingNumber { get; set; } = string.Empty;
    public string HoldingIdentifierType { get; set; } = string.Empty;

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