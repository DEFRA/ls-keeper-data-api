using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Silver;

/// <summary>
/// Composite key: Source, HoldingIdentifier, PartyId, RoleTypeId
/// </summary>
[CollectionName("silverSitePartyRoleRelationships")]
public class SitePartyRoleRelationshipDocument : IEntity, IContainsIndexes
{
    [BsonId]
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public string? Id { get; set; }

    [JsonPropertyName("partyId")]
    [BsonElement("partyId")]
    public string PartyId { get; set; } = string.Empty;

    [JsonPropertyName("partyTypeId")]
    [BsonElement("partyTypeId")]
    public string PartyTypeId { get; set; } = string.Empty; // LOV Lookup / Internal Id

    [JsonPropertyName("holdingIdentifier")]
    [BsonElement("holdingIdentifier")]
    public string HoldingIdentifier { get; set; } = string.Empty;

    [JsonPropertyName("holdingIdentifierType")]
    [BsonElement("holdingIdentifierType")]
    public string HoldingIdentifierType { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    [BsonElement("source")]
    public string Source { get; set; } = string.Empty; // Enum or string value

    [JsonPropertyName("roleTypeId")]
    [BsonElement("roleTypeId")]
    public string? RoleTypeId { get; set; } // LOV Lookup / Internal Id

    [JsonPropertyName("roleTypeName")]
    [BsonElement("roleTypeName")]
    public string? RoleTypeName { get; set; } // LOV Lookup / Internal Name

    [JsonPropertyName("sourceRoleName")]
    [BsonElement("sourceRoleName")]
    public string? SourceRoleName { get; set; }

    [JsonPropertyName("effectiveFromData")]
    [BsonElement("effectiveFromData")]
    public DateTime? EffectiveFromData { get; set; }

    [JsonPropertyName("effectiveToData")]
    [BsonElement("effectiveToData")]
    public DateTime? EffectiveToData { get; set; }

    [JsonPropertyName("lastUpdatedBatchId")]
    [BsonElement("lastUpdatedBatchId")]
    public int? LastUpdatedBatchId { get; set; }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("holdingIdentifier"),
                new CreateIndexOptions { Name = "idx_holdingIdentifier" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("holdingIdentifierType"),
                new CreateIndexOptions { Name = "idx_holdingIdentifierType" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("partyId"),
                new CreateIndexOptions { Name = "idx_partyId" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("partyTypeId"),
                new CreateIndexOptions { Name = "idx_partyTypeId" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("roleTypeId"),
                new CreateIndexOptions { Name = "idx_roleTypeId" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("source"),
                new CreateIndexOptions { Name = "idx_source" })
        ];
    }
}