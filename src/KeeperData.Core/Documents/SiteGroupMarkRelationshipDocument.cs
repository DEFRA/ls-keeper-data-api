using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KeeperData.Core.Documents;

[CollectionName("siteGroupMarkRelationships")]
public class SiteGroupMarkRelationshipDocument : IEntity, IContainsIndexes, IDeletableEntity
{
    public string? Id { get; set; }
    public int? LastUpdatedBatchId { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public bool Deleted { get; set; }

    public string Herdmark { get; set; } = string.Empty;
    public string CountyParishHoldingHerd { get; set; } = string.Empty;
    public string CountyParishHoldingNumber { get; set; } = string.Empty;

    // TODO - Add remaining fields

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("HoldingIdentifier"),
                new CreateIndexOptions { Name = "idx_holdingIdentifier" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("HoldingIdentifierType"),
                new CreateIndexOptions { Name = "idx_holdingIdentifierType" })
        ];
    }
}