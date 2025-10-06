using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Parties;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace KeeperData.Core.Documents;

[CollectionName("parties")]
public class PartyDocument : IEntity, IDeletableEntity, IContainsIndexes
{
    [BsonId]
    public required string Id { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Name { get; set; }
    public bool Deleted { get; set; }

    public static PartyDocument FromDomain(Party m) => new()
    {
        Id = m.Id,
        LastUpdatedDate = m.LastUpdatedDate,
        FirstName = m.FirstName,
        LastName = m.LastName,
        Name = m.Name
    };

    public Party ToDomain()
    {
        var site = new Party(
            Id,
            LastUpdatedDate,
            FirstName,
            LastName,
            Name);

        return site;
    }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("Name"),
                new CreateIndexOptions { Name = "idx_name" }),
        ];
    }
}