using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

[CollectionName("sites")]
public class SiteDocument : IEntity, IDeletableEntity, IContainsIndexes
{
    [BsonId]
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; private set; }

    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("state")]
    public string? State { get; set; } = default!;

    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("destroyIdentityDocumentsFlag")]
    public bool? DestroyIdentityDocumentsFlag { get; set; }

    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }

    [JsonPropertyName("location")]
    public LocationDocument? Location { get; set; }

    [JsonPropertyName("identifiers")]
    public List<SiteIdentifierDocument> Identifiers { get; private set; } = [];

    [JsonPropertyName("parties")]
    public List<SitePartyDocument> Parties { get; set; } = [];

    [JsonPropertyName("species")]
    public List<SpeciesSummaryDocument> Species { get; set; } = [];

    [JsonPropertyName("marks")]
    public List<GroupMarkDocument> Marks { get; set; } = [];

    [JsonPropertyName("siteActivities")]
    public List<SiteActivityDocument> SiteActivities { get; set; } = [];

    [JsonPropertyName("activities")]
    public List<string> Activities { get; set; } = [];

    public static SiteDocument FromDomain(Site m) => new()
    {
        Id = m.Id,
        CreatedDate = m.CreatedDate,
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
        Deleted = m.Deleted,
        Parties = [.. m.Parties.Select(SitePartyDocument.FromDomain)],
        Species = [.. m.Species.Select(SpeciesSummaryDocument.FromDomain)],
        Marks = [.. m.Marks.Select(GroupMarkDocument.FromDomain)],
        SiteActivities = [.. m.Activities.Select(SiteActivityDocument.FromDomain)],
        Activities = [.. m.Activities.Select(a => a.Description ?? string.Empty)]
    };

    public Site ToDomain()
    {
        var site = new Site(
            Id,
            CreatedDate,
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
                Builders<BsonDocument>.IndexKeys.Ascending("CreatedDate"),
                new CreateIndexOptions { Name = "idx_createdDate" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("LastUpdatedDate"),
                new CreateIndexOptions { Name = "idx_lastUpdatedDate" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("identifiers.identifier"),
                new CreateIndexOptions { Name = "idx_identifiers_identifier", Sparse = true })
        ];
    }
}