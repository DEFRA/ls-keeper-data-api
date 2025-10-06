using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KeeperData.Core.Documents.Silver;

[CollectionName("ctsHoldings")]
public class CtsHoldingDocument : IEntity, IDeletableEntity, IContainsIndexes
{
    public string Id { get; set; } = string.Empty;
    public string CountyParishHoldingNumber { get; set; } = string.Empty;
    public string? AlternativeHoldingIdentifier { get; set; }
    public bool Deleted { get; set; }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("CountyParishHoldingNumber"),
                new CreateIndexOptions { Name = "idx_cphNumber" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("AlternativeHoldingIdentifier"),
                new CreateIndexOptions { Name = "idx_altIdentifier" })
        ];
    }
}