using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refCountries")]
public class CountryListDocument : IListDocument, IReferenceListDocument<CountryDocument>
{
    [BsonId]
    public string Id { get; set; } = "all-countries";

    public int LastUpdatedBatchId { get; set; }

    public DateTime LastUpdatedDate { get; set; }

    public List<CountryDocument> Countries { get; set; } = [];

    public IReadOnlyCollection<CountryDocument> Items => Countries.AsReadOnly();
}