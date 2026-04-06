using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("referenceData")]
public class SiteTypeListDocument : IListDocument, IReferenceListDocument<SiteTypeDocument>
{
    public static string DocumentId => "all-sitetypes";

    [BsonId]
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = DocumentId;

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("siteTypes")]
    [JsonPropertyName("siteTypes")]
    public List<SiteTypeDocument> SiteTypes { get; set; } = [];

    public IReadOnlyCollection<SiteTypeDocument> Items => SiteTypes.AsReadOnly();
}