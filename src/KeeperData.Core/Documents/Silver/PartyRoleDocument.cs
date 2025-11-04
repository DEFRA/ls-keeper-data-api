using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Silver;

public class PartyRoleDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }

    public string? RoleTypeId { get; set; } // LOV Lookup / Internal Id
    public string? RoleTypeName { get; set; } // LOV Lookup / Internal Name
    public string? SourceRoleName { get; set; }

    public DateTime? EffectiveFromDate { get; set; }
    public DateTime? EffectiveToDate { get; set; }
}