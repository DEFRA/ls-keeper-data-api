using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("referenceData")]
public class SiteTypeMapListDocument : IListDocument, IReferenceListDocument<SiteTypeMapDocument>
{
    public static string DocumentId => "all-sitetypemaps";

    [BsonId]
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = DocumentId;

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("siteTypeMaps")]
    [JsonPropertyName("siteTypeMaps")]
    public List<SiteTypeMapDocument> SiteTypeMaps { get; set; } = [];

    [JsonIgnore]
    public IReadOnlyCollection<SiteTypeMapDocument> Items => SiteTypeMaps.AsReadOnly();
}
