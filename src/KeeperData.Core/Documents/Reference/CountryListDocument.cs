using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refCountries")]
public class CountryListDocument : IListDocument
{
    [BsonId]
    public string Id { get; set; } = "all-countries";

    public DateTime LastUpdatedDate { get; set; }

    public List<CountryDocument> Countries { get; set; } = [];
}