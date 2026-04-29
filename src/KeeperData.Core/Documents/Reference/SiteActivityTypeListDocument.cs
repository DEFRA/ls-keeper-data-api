using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("referenceData")]
public class SiteActivityTypeListDocument : IListDocument, IReferenceListDocument<SiteActivityTypeDocument>
{
    public static string DocumentId => "all-siteactivitytypes";

    [BsonId]
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = DocumentId;

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("siteActivityTypes")]
    [JsonPropertyName("siteActivityTypes")]
    public List<SiteActivityTypeDocument> SiteActivityTypes { get; set; } = [];

    public IReadOnlyCollection<SiteActivityTypeDocument> Items => SiteActivityTypes.AsReadOnly();
}