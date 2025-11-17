using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refPremisesTypes")]
public class PremisesTypeListDocument : IListDocument
{
    [BsonId]
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = "all-premisestypes";

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("premisesTypes")]
    [JsonPropertyName("premisesTypes")]
    public List<PremisesTypeDocument> PremisesTypes { get; set; } = [];
}