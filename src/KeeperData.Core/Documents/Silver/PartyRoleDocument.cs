using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Silver;

public class PartyRoleDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }

    public string? RoleTypeId { get; set; } // LOV Lookup
    public string? RoleName { get; set; }

    public DateTime EffectiveFromData { get; set; } = default;
    public DateTime? EffectiveToData { get; set; }
}
