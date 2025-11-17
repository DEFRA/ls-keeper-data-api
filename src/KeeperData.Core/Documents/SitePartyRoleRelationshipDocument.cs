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
    public string? Id { get; set; }

    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }

    [JsonPropertyName("partyId")]
    public string PartyId { get; set; } = string.Empty;

    [JsonPropertyName("partyTypeId")]
    public string PartyTypeId { get; set; } = string.Empty;

    [JsonPropertyName("isHolder")]
    public bool IsHolder { get; set; }

    [JsonPropertyName("holdingIdentifier")]
    public string HoldingIdentifier { get; set; } = string.Empty;

    [JsonPropertyName("holdingIdentifierType")]
    public string HoldingIdentifierType { get; set; } = string.Empty;

    [JsonPropertyName("roleTypeId")]
    public string? RoleTypeId { get; set; }

    [JsonPropertyName("roleTypeName")]
    public string? RoleTypeName { get; set; }

    [JsonPropertyName("effectiveFromData")]
    public DateTime? EffectiveFromData { get; set; }

    [JsonPropertyName("effectiveToData")]
    public DateTime? EffectiveToData { get; set; }

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