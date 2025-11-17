using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refPremisesActivityTypes")]
public class PremisesActivityTypeListDocument : IListDocument
{
    [BsonId]
    public string Id { get; set; } = "all-premisesactivitytypes";

    public DateTime LastUpdatedDate { get; set; }

    public List<PremisesActivityTypeDocument> PremisesActivityTypes { get; set; } = [];
}