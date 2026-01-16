using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("referenceData")]
public class RoleListDocument : IListDocument, IReferenceListDocument<RoleDocument>
{
    public static string DocumentId => "all-roles";

    [BsonId]
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = DocumentId;

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("roles")]
    [JsonPropertyName("roles")]
    public List<RoleDocument> Roles { get; set; } = [];

    public IReadOnlyCollection<RoleDocument> Items => Roles.AsReadOnly();
}