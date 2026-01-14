using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("referenceData")]
public class SpeciesListDocument : IListDocument, IReferenceListDocument<SpeciesDocument>
{
    public static string DocumentId => "all-species";

    [BsonId]
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = DocumentId;

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("species")]
    [JsonPropertyName("species")]
    public List<SpeciesDocument> Species { get; set; } = [];

    public IReadOnlyCollection<SpeciesDocument> Items => Species.AsReadOnly();
}