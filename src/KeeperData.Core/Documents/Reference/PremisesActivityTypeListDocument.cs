using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("referenceData")]
public class PremisesActivityTypeListDocument : IListDocument, IReferenceListDocument<PremisesActivityTypeDocument>
{
    public static string DocumentId => "all-premisesactivitytypes";

    [BsonId]
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = DocumentId;

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("premisesActivityTypes")]
    [JsonPropertyName("premisesActivityTypes")]
    public List<PremisesActivityTypeDocument> PremisesActivityTypes { get; set; } = [];

    public IReadOnlyCollection<PremisesActivityTypeDocument> Items => PremisesActivityTypes.AsReadOnly();
}