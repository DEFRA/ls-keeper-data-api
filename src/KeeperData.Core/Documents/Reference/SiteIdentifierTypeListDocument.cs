using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refSiteIdentifierTypes")]
public class SiteIdentifierTypeListDocument : IListDocument
{
    [BsonId]
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = "all-siteidentifiertypes";

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("siteIdentifierTypes")]
    [JsonPropertyName("siteIdentifierTypes")]
    public List<SiteIdentifierTypeDocument> SiteIdentifierTypes { get; set; } = [];
}