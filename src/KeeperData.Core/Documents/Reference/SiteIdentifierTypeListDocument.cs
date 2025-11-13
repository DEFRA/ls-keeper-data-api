using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refSiteIdentifierTypes")]
public class SiteIdentifierTypeListDocument : IListDocument, IReferenceListDocument<SiteIdentifierTypeDocument>
{
    public static string DocumentId => "all-siteidentifiertypes";

    [BsonId]
    public string Id { get; set; } = DocumentId;

    public int? LastUpdatedBatchId { get; set; }

    public DateTime LastUpdatedDate { get; set; }

    public List<SiteIdentifierTypeDocument> SiteIdentifierTypes { get; set; } = [];

    public IReadOnlyCollection<SiteIdentifierTypeDocument> Items => SiteIdentifierTypes.AsReadOnly();
}