using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace KeeperData.Core.Documents;

[CollectionName("sites")]
public class SiteDocument : IEntity, IDeletableEntity, IContainsIndexes
{
    [BsonId]
    public required string Id { get; set; }
    public int? LastUpdatedBatchId { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public string Type { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? State { get; set; } = default!;
    public List<SiteIdentifierDocument> Identifiers { get; private set; } = [];
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Source { get; set; }
    public bool? DestroyIdentityDocumentsFlag { get; set; }
    public LocationDocument? Location { get; set; }
    public bool Deleted { get; set; }

    // TODO
    // public List<SitePartyDocument> Parties { get; set; } = [];
    // public List<SiteSpeciesDocument> Species { get; set; } = [];
    // public List<SiteGroupMarkDocument> Marks { get; set; } = [];
    // public List<string> Activities { get; set; } = [];

    public static SiteDocument FromDomain(Site m) => new()
    {
        Id = m.Id,
        LastUpdatedBatchId = m.LastUpdatedBatchId,
        LastUpdatedDate = m.LastUpdatedDate,
        Type = m.Type,
        Name = m.Name,
        State = m.State,
        Identifiers = [.. m.Identifiers.Select(SiteIdentifierDocument.FromDomain)],
        Location = m.Location is not null ? LocationDocument.FromDomain(m.Location) : null,
        StartDate = m.StartDate,
        EndDate = m.EndDate,
        Source = m.Source,
        DestroyIdentityDocumentsFlag = m.DestroyIdentityDocumentsFlag,
        Deleted = m.Deleted //,
        // Parties = [.. m.Parties.Select(SitePartyDocument.FromDomain)],
        // Species = [.. m.Species.Select(SpeciesDocument.FromDomain)],
        // Marks = [.. m.Marks.Select(GroupMarkDocument.FromDomain)],
        // Activities = [.. m.Activities.Select(SiteActivityDocument.FromDomain)],
    };

    public Site ToDomain()
    {
        var site = new Site(
            Id,
            LastUpdatedBatchId,
            LastUpdatedDate,
            Type,
            Name,
            StartDate,
            EndDate,
            State,
            Source,
            DestroyIdentityDocumentsFlag,
            Deleted,
            null
        );

        foreach (var si in Identifiers)
        {
            site.SetSiteIdentifier(
                si.LastUpdatedDate,
                si.Identifier,
                si.Type,
                si.IdentifierId);
        }

        if (Location is not null)
        {
            site.SetLocation(Location.ToDomain());
        }

        // TODO: Add Parties, Species, Marks, Activities etc

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