using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refRoles")]
public class RoleListDocument : IListDocument
{
    [BsonId]
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = "all-roles";

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("roles")]
    [JsonPropertyName("roles")]
    public List<RoleDocument> Roles { get; set; } = [];
}