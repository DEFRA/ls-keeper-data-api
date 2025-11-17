using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refRoles")]
public class RoleListDocument : IListDocument, IReferenceListDocument<RoleDocument>
{
    public static string DocumentId => "all-roles";

    [BsonId]
    public string Id { get; set; } = DocumentId;

    public int? LastUpdatedBatchId { get; set; }

    public DateTime LastUpdatedDate { get; set; }

    public List<RoleDocument> Roles { get; set; } = [];

    public IReadOnlyCollection<RoleDocument> Items => Roles.AsReadOnly();
}