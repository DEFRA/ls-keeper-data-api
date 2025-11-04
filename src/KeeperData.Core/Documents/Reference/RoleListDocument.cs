using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refRoles")]
public class RoleListDocument : IListDocument
{
    [BsonId]
    public string Id { get; set; } = "all-roles";

    public DateTime LastUpdatedDate { get; set; }

    public List<RoleDocument> Roles { get; set; } = [];
}