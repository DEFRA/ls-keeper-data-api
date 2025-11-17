using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refPremisesTypes")]
public class PremisesTypeListDocument : IListDocument, IReferenceListDocument<PremisesTypeDocument>
{
    public static string DocumentId => DocumentId;

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

    public IReadOnlyCollection<PremisesTypeDocument> Items => PremisesTypes.AsReadOnly();
}