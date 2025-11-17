using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Silver;

public class PartyRoleDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }

    [JsonPropertyName("roleTypeId")]
    [BsonElement("roleTypeId")]
    public string? RoleTypeId { get; set; } // LOV Lookup / Internal Id

    [JsonPropertyName("roleTypeName")]
    [BsonElement("roleTypeName")]
    public string? RoleTypeName { get; set; } // LOV Lookup / Internal Name

    [JsonPropertyName("sourceRoleName")]
    [BsonElement("sourceRoleName")]
    public string? SourceRoleName { get; set; }

    [JsonPropertyName("effectiveFromDate")]
    [BsonElement("effectiveFromDate")]
    public DateTime? EffectiveFromDate { get; set; }

    [JsonPropertyName("effectiveToDate")]
    [BsonElement("effectiveToDate")]
    public DateTime? EffectiveToDate { get; set; }
}