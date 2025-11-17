using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refSiteIdentifierTypes")]
public class SiteIdentifierTypeListDocument : IListDocument, IReferenceListDocument<SiteIdentifierTypeDocument>
{
    public static string DocumentId => "all-siteidentifiertypes";

    [BsonId]
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = DocumentId;

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("siteIdentifierTypes")]
    [JsonPropertyName("siteIdentifierTypes")]
    public List<SiteIdentifierTypeDocument> SiteIdentifierTypes { get; set; } = [];

    public IReadOnlyCollection<SiteIdentifierTypeDocument> Items => SiteIdentifierTypes.AsReadOnly();
}