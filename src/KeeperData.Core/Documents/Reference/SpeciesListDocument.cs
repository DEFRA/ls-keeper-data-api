using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using MongoDB.Bson.Serialization.Attributes;

[CollectionName("refSpecies")]
public class SpeciesListDocument : IListDocument
{
    [BsonId]
    public string Id { get; set; } = "all-species";

    public DateTime LastUpdatedDate { get; set; }

    public List<SpeciesDocument> Species { get; set; } = [];
}