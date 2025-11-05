using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refProductionUsages")]
public class ProductionUsageListDocument : IListDocument
{
    [BsonId]
    public string Id { get; set; } = "all-productionusages";

    public DateTime LastUpdatedDate { get; set; }

    public List<ProductionUsageDocument> ProductionUsages { get; set; } = [];
}