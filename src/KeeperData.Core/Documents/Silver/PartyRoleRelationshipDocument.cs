using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KeeperData.Core.Documents.Silver;

[CollectionName("partyRoleRelationships")]
public class PartyRoleRelationshipDocument : IEntity, IContainsIndexes
{
    public string? Id { get; set; }
    public string PartyId { get; set; } = string.Empty;
    public string PartyTypeId { get; set; } = string.Empty; // LOV Lookup / Internal Id
    public string HoldingIdentifier { get; set; } = string.Empty;
    public string HoldingIdentifierType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // Enum or string value

    public string? RoleTypeId { get; set; } // LOV Lookup / Internal Id
    public string? RoleTypeName { get; set; } // LOV Lookup / Internal Name
    public string? SourceRoleName { get; set; }

    public DateTime? EffectiveFromData { get; set; }
    public DateTime? EffectiveToData { get; set; }

    public int LastUpdatedBatchId { get; set; }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("HoldingIdentifier"),
                new CreateIndexOptions { Name = "idx_holdingIdentifier" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("HoldingIdentifierType"),
                new CreateIndexOptions { Name = "idx_holdingIdentifierType" }),

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