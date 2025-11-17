using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

[CollectionName("refSpecies")]
public class SpeciesListDocument : IListDocument
{
    [BsonId]
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = "all-species";

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("species")]
    [JsonPropertyName("species")]
    public List<SpeciesDocument> Species { get; set; } = [];
}