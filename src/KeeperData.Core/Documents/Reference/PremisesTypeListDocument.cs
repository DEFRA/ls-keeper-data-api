using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("referenceData")]
public class PremisesTypeListDocument : IListDocument, IReferenceListDocument<PremisesTypeDocument>
{
    public static string DocumentId => "all-premisestypes";

    [BsonId]
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = DocumentId;

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("premisesTypes")]
    [JsonPropertyName("premisesTypes")]
    public List<PremisesTypeDocument> PremisesTypes { get; set; } = [];

    public IReadOnlyCollection<PremisesTypeDocument> Items => PremisesTypes.AsReadOnly();
}