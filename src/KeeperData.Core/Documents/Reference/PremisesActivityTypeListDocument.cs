using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refPremisesActivityTypes")]
public class PremisesActivityTypeListDocument : IListDocument
{
    [BsonId]
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = "all-premisesactivitytypes";

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("premisesActivityTypes")]
    [JsonPropertyName("premisesActivityTypes")]
    public List<PremisesActivityTypeDocument> PremisesActivityTypes { get; set; } = [];
}