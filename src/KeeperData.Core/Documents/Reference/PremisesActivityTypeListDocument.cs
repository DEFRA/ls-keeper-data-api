using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refPremisesActivityTypes")]
public class PremisesActivityTypeListDocument : IListDocument, IReferenceListDocument<PremisesActivityTypeDocument>
{
    public static string DocumentId => "all-premisesactivitytypes";

    [BsonId]
    public string Id { get; set; } = DocumentId;

    public int? LastUpdatedBatchId { get; set; }

    public DateTime LastUpdatedDate { get; set; }

    public List<PremisesActivityTypeDocument> PremisesActivityTypes { get; set; } = [];

    public IReadOnlyCollection<PremisesActivityTypeDocument> Items => PremisesActivityTypes.AsReadOnly();
}