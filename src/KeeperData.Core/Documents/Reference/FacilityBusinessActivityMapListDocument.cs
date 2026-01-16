using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("referenceData")]
public class FacilityBusinessActivityMapListDocument : IListDocument, IReferenceListDocument<FacilityBusinessActivityMapDocument>
{
    public static string DocumentId => "all-facilitybusinessactivitymaps";

    [BsonId]
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = DocumentId;

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("facilityBusinessActivityMaps")]
    [JsonPropertyName("facilityBusinessActivityMaps")]
    public List<FacilityBusinessActivityMapDocument> FacilityBusinessActivityMaps { get; set; } = [];

    [JsonIgnore]
    public IReadOnlyCollection<FacilityBusinessActivityMapDocument> Items => FacilityBusinessActivityMaps.AsReadOnly();
}