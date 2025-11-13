using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refProductionUsages")]
public class ProductionUsageListDocument : IListDocument, IReferenceListDocument<ProductionUsageDocument>
{
    public static string DocumentId => "all-productionusages";

    [BsonId]
    public string Id { get; set; } = DocumentId;

    public int? LastUpdatedBatchId { get; set; }

    public DateTime LastUpdatedDate { get; set; }

    public List<ProductionUsageDocument> ProductionUsages { get; set; } = [];

    public IReadOnlyCollection<ProductionUsageDocument> Items => ProductionUsages.AsReadOnly();
}