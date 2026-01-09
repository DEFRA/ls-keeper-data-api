using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Silver;

/// <summary>
/// Composite key: Source, HoldingIdentifier, PartyId, RoleTypeId
/// </summary>
public class SitePartyRoleRelationshipDocument : IEntity
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

    [JsonPropertyName("source")]
    [BsonElement("source")]
    public string Source { get; set; } = string.Empty; // Enum or string value

    [JsonPropertyName("roleTypeId")]
    [BsonElement("roleTypeId")]
    public string? RoleTypeId { get; set; } // LOV Lookup / Internal Id

    [JsonPropertyName("roleTypeCode")]
    [BsonElement("roleTypeCode")]
    public string? RoleTypeCode { get; set; }

    [JsonPropertyName("roleTypeName")]
    [BsonElement("roleTypeName")]
    public string? RoleTypeName { get; set; }

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
}