using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

/// <summary>
/// Composite key: HoldingIdentifier, IsHolder, PartyId, RoleTypeId
/// </summary>
[CollectionName("sitePartyRoleRelationships")]
public class SitePartyRoleRelationshipDocument : IEntity, IContainsIndexes
{
    [BsonId]
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public string? Id { get; set; }

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("deleted")]
    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }

    [BsonElement("partyId")]
    [JsonPropertyName("partyId")]
    public string PartyId { get; set; } = string.Empty;

    [BsonElement("partyTypeId")]
    [JsonPropertyName("partyTypeId")]
    public string PartyTypeId { get; set; } = string.Empty;

    [BsonElement("isHolder")]
    [JsonPropertyName("isHolder")]
    public bool IsHolder { get; set; }

    [BsonElement("holdingIdentifier")]
    [JsonPropertyName("holdingIdentifier")]
    public string HoldingIdentifier { get; set; } = string.Empty;

    [BsonElement("holdingIdentifierType")]
    [JsonPropertyName("holdingIdentifierType")]
    public string HoldingIdentifierType { get; set; } = string.Empty;

    [BsonElement("roleTypeId")]
    [JsonPropertyName("roleTypeId")]
    public string? RoleTypeId { get; set; }

    [BsonElement("roleTypeName")]
    [JsonPropertyName("roleTypeName")]
    public string? RoleTypeName { get; set; }

    [BsonElement("effectiveFromData")]
    [JsonPropertyName("effectiveFromData")]
    public DateTime? EffectiveFromData { get; set; }

    [BsonElement("effectiveToData")]
    [JsonPropertyName("effectiveToData")]
    public DateTime? EffectiveToData { get; set; }

    [BsonElement("speciesManagedByRole")]
    [JsonPropertyName("speciesManagedByRole")]
    public List<ManagedSpeciesDocument> SpeciesManagedByRole { get; set; } = [];

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
                new CreateIndexOptions { Name = "idx_roleTypeId" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("IsHolder"),
                new CreateIndexOptions { Name = "idx_isHolder" })
        ];
    }
}