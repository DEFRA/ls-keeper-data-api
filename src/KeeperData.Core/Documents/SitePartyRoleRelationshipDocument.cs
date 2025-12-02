using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

/// <summary>
/// Composite key: HoldingIdentifier, PartyId, RoleTypeId, SpeciesTypeId
/// </summary>
[CollectionName("sitePartyRoleRelationships")]
public record SitePartyRoleRelationshipDocument : IEntity, IContainsIndexes
{
    [BsonId]
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public string? Id { get; set; }

    [BsonElement("partyId")]
    [JsonPropertyName("partyId")]
    [AutoIndexed]
    public string PartyId { get; set; } = string.Empty;

    [BsonElement("partyTypeId")]
    [JsonPropertyName("partyTypeId")]
    [AutoIndexed]
    public string PartyTypeId { get; set; } = string.Empty;

    [BsonElement("holdingIdentifier")]
    [JsonPropertyName("holdingIdentifier")]
    [AutoIndexed]
    public string HoldingIdentifier { get; set; } = string.Empty;

    [BsonElement("holdingIdentifierType")]
    [JsonPropertyName("holdingIdentifierType")]
    [AutoIndexed]
    public string HoldingIdentifierType { get; set; } = string.Empty;

    [BsonElement("roleTypeId")]
    [JsonPropertyName("roleTypeId")]
    [AutoIndexed]
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

    [BsonElement("speciesTypeId")]
    [JsonPropertyName("speciesTypeId")]
    [AutoIndexed]
    public string? SpeciesTypeId { get; set; }

    [BsonElement("speciesTypeCode")]
    [JsonPropertyName("speciesTypeCode")]
    [AutoIndexed]
    public string? SpeciesTypeCode { get; set; }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return AutoIndexedAttribute.GetIndexModels<SitePartyRoleRelationshipDocument>();
    }
}