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
    [AutoIndexed]
    public string PartyId { get; set; } = string.Empty;

    [JsonPropertyName("partyTypeId")]
    [BsonElement("partyTypeId")]
    [AutoIndexed]
    public string PartyTypeId { get; set; } = string.Empty; // LOV Lookup / Internal Id

    [JsonPropertyName("holdingIdentifier")]
    [BsonElement("holdingIdentifier")]
    [AutoIndexed]
    public string HoldingIdentifier { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    [BsonElement("source")]
    [AutoIndexed]
    public string Source { get; set; } = string.Empty; // Enum or string value

    [JsonPropertyName("roleTypeId")]
    [BsonElement("roleTypeId")]
    [AutoIndexed]
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
        return AutoIndexedAttribute.GetIndexModels<SitePartyRoleRelationshipDocument>();
    }
}