using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refSiteIdentifierTypes")]
public class SiteIdentifierTypeListDocument : IListDocument
{
    [BsonId]
    public string Id { get; set; } = "all-siteidentifiertypes";

    public DateTime LastUpdatedDate { get; set; }

    public List<SiteIdentifierTypeDocument> SiteIdentifierTypes { get; set; } = [];
}