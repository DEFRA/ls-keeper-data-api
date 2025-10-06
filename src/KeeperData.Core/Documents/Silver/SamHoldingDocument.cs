using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KeeperData.Core.Documents.Silver;

[CollectionName("samHoldings")]
public class SamHoldingDocument : IEntity, IDeletableEntity, IContainsIndexes
{
    public string Id { get; set; } = string.Empty;
    public string CountyParishHoldingNumber { get; set; } = string.Empty;
    public string? AlternativeHoldingIdentifier { get; set; }
    public string? CphTypeIdentifier { get; set; }
    public string? HoldingStartDate { get; set; }
    public string? HoldingEndDate { get; set; }
    public string? PremiseActivityTypeId { get; set; }
    public string? PremiseTypeIdentifier { get; set; }
    public string? LocationName { get; set; }
    public LocationDocument? Location { get; set; }
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