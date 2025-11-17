using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refSpecies")]
public class SpeciesListDocument : IListDocument, IReferenceListDocument<SpeciesDocument>
{
    public static string DocumentId => "all-species";

    [BsonId]
    public string Id { get; set; } = DocumentId;

    public int? LastUpdatedBatchId { get; set; }

    public DateTime LastUpdatedDate { get; set; }

    public List<SpeciesDocument> Species { get; set; } = [];

    public IReadOnlyCollection<SpeciesDocument> Items => Species.AsReadOnly();
}