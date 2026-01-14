using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("referenceData")]
public class CountryListDocument : IListDocument, IReferenceListDocument<CountryDocument>
{
    public static string DocumentId => "all-countries";

    [BsonId]
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = DocumentId;

    [BsonElement("lastUpdatedBatchId")]
    [JsonPropertyName("lastUpdatedBatchId")]
    public int? LastUpdatedBatchId { get; set; }

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("countries")]
    [JsonPropertyName("countries")]
    public List<CountryDocument> Countries { get; set; } = [];

    [JsonIgnore]
    public IReadOnlyCollection<CountryDocument> Items => Countries.AsReadOnly();
}