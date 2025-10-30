using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refPartyRoles")]
public class PartyRoleListDocument : IListDocument
{
    [BsonId]
    public string Id { get; set; } = "all-partyroles";

    public DateTime LastUpdatedDate { get; set; }

    public List<PartyRoleDocument> PartyRoles { get; set; } = [];
}