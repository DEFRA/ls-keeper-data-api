using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KeeperData.Core.Documents;

[CollectionName("sites")]
public class SiteDocument : IEntity, IContainsIndexes
{
    [BsonId]
    public required string Id { get; set; }

    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<SiteIdentifierDocument> Identifiers { get; set; } = [];
    public string? State { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Source { get; set; }
    public bool? DestroyIdentityDocumentsFlag { get; set; }
    public LocationDocument? Location { get; set; }
    public List<SitePartyDocument> Parties { get; set; } = [];
    public List<SpeciesDocument> Species { get; set; } = [];
    public List<MarksDocument> Marks { get; set; } = [];
    public List<SiteActivityDocument> Activities { get; set; } = [];
    public List<string> KeeperPartyIds { get; set; } = [];

    public static SiteDocument FromDomain(Site m) => new()
    {
        Id = m.Id,
        LastUpdatedDate = m.LastUpdatedDate,
        Type = m.Type,
        Name = m.Name,
        State = m.State,
        StartDate = m.StartDate,
        EndDate = m.EndDate,
        Source = m.Source,
        DestroyIdentityDocumentsFlag = m.DestroyIdentityDocumentsFlag,
        Identifiers = m.Identifiers.Select(SiteIdentifierDocument.FromDomain).ToList(),
        Location = m.Location is not null ? LocationDocument.FromDomain(m.Location) : null,
        Parties = m.Parties.Select(SitePartyDocument.FromDomain).ToList(),
        Species = m.Species.Select(SpeciesDocument.FromDomain).ToList(),
        Marks = m.Marks.Select(MarksDocument.FromDomain).ToList(),
        Activities = m.Activities.Select(SiteActivityDocument.FromDomain).ToList(),
    };


    public Site ToDomain()
    {
        var site = new Site(
            Id,
            LastUpdatedDate,
            Type ?? string.Empty,
            Name ?? string.Empty,
            StartDate,
            State,
            EndDate,
            Source,
            DestroyIdentityDocumentsFlag,
            Location?.ToDomain()
        );

        site.LoadIdentifiers(Identifiers.Select(doc => doc.ToDomain()));
        site.LoadParties(Parties.Select(doc => doc.ToDomain()));
        site.LoadSpecies(Species.Select(doc => doc.ToDomain()));
        site.LoadMarks(Marks.Select(doc => doc.ToDomain()));
        site.LoadActivities(Activities.Select(doc => doc.ToDomain()));

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