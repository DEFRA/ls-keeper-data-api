using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KeeperData.Core.Documents.Silver;

/// <summary>
/// Composite key: PartyId
/// </summary>
[CollectionName("samParties")]
public class SamPartyDocument : BasePartyDocument, IEntity, IDeletableEntity, IContainsIndexes
{
    public string? Id { get; set; }
    public int LastUpdatedBatchId { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public bool Deleted { get; set; }
    public bool IsHolder { get; set; }

    public List<string> CphList { get; set; } = [];

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