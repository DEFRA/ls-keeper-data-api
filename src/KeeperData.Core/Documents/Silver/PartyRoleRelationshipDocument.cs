using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KeeperData.Core.Documents.Silver;

[CollectionName("partyRoleRelationships")]
public class PartyRoleRelationshipDocument : IEntity, IContainsIndexes
{
    public string Id { get; set; } = string.Empty;
    public string CountyParishHoldingNumber { get; set; } = string.Empty;

    public string PartyId { get; set; } = string.Empty;
    public string PartyTypeId { get; set; } = string.Empty; // LOV Lookup

    public string? RoleTypeId { get; set; } // LOV Lookup
    public string? RoleName { get; set; }

    public DateTime EffectiveFromData { get; set; } = default;
    public DateTime? EffectiveToData { get; set; }

    public int LastUpdatedBatchId { get; set; }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("CountyParishHoldingNumber"),
                new CreateIndexOptions { Name = "idx_cphNumber" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("PartyId"),
                new CreateIndexOptions { Name = "idx_partyId" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("PartyTypeId"),
                new CreateIndexOptions { Name = "idx_partyTypeId" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("RoleTypeId"),
                new CreateIndexOptions { Name = "idx_roleTypeId" })
        ];
    }
}
