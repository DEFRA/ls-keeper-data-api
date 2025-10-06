using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace KeeperData.Core.Documents;

[CollectionName("sites")]
public class SiteDocument : IEntity, IContainsIndexes
{
    [BsonId]
    public required string Id { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public string Type { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string State { get; set; } = default!;
    public List<SiteIdentifierDocument> Identifiers { get; private set; } = [];
    public LocationDocument? Location { get; set; }
    public List<string> KeeperPartyIds { get; set; } = [];

    public static SiteDocument FromDomain(Site m) => new()
    {
        Id = m.Id,
        LastUpdatedDate = m.LastUpdatedDate,
        Type = m.Type,
        Name = m.Name,
        State = m.State,
        Identifiers = [.. m.Identifiers.Select(SiteIdentifierDocument.FromDomain)],
        Location = m.Location is not null ? LocationDocument.FromDomain(m.Location) : null
    };

    public Site ToDomain()
    {
        var site = new Site(
            Id,
            LastUpdatedDate,
            Type,
            Name,
            State);

        foreach (var si in Identifiers)
        {
            site.AddSiteIdentifier(
                si.LastUpdatedDate,
                si.Identifier,
                si.Type,
                si.IdentifierId);
        }

        if (Location is not null)
        {
            site.SetLocation(
                Location.LastUpdatedDate,
                Location.OsMapReference,
                Location.Easting,
                Location.Northing,
                Location.IdentifierId);
        }

        return site;
    }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("type"),
                new CreateIndexOptions { Name = "idx_type" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("name"),
                new CreateIndexOptions { Name = "idx_name" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("state"),
                new CreateIndexOptions { Name = "idx_state" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("keeperPartyIds"),
                new CreateIndexOptions { Name = "idx_keeperPartyIds" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("identifiers.identifier"),
                new CreateIndexOptions { Name = "idx_identifiers_identifier", Sparse = true })
        ];
    }
}