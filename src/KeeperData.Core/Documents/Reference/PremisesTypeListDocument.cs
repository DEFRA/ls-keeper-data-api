using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refPremisesTypes")]
public class PremisesTypeListDocument : IListDocument, IReferenceListDocument<PremisesTypeDocument>
{
    public static string DocumentId => "all-premisestypes";

    [BsonId]
    public string Id { get; set; } = DocumentId;

    public int? LastUpdatedBatchId { get; set; }

    public DateTime LastUpdatedDate { get; set; }

    public List<PremisesTypeDocument> PremisesTypes { get; set; } = [];

    public IReadOnlyCollection<PremisesTypeDocument> Items => PremisesTypes.AsReadOnly();
}