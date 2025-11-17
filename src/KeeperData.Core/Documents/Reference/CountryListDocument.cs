using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refCountries")]
public class CountryListDocument : IListDocument, IReferenceListDocument<CountryDocument>
{
    public static string DocumentId => "all-countries";

    [BsonId]
    public string Id { get; set; } = DocumentId;

    public int? LastUpdatedBatchId { get; set; }

    public DateTime LastUpdatedDate { get; set; }

    public List<CountryDocument> Countries { get; set; } = [];

    public IReadOnlyCollection<CountryDocument> Items => Countries.AsReadOnly();
}