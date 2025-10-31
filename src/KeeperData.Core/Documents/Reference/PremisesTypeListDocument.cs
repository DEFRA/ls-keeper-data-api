using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refPremisesTypes")]
public class PremisesTypeListDocument : IListDocument
{
    [BsonId]
    public string Id { get; set; } = "all-premisestypes";

    public DateTime LastUpdatedDate { get; set; }

    public List<PremisesTypeDocument> PremisesTypes { get; set; } = [];
}