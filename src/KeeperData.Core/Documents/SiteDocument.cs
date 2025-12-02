using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Shared;
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
    [BsonElement("id")]
    public required string Id { get; set; }

    [BsonElement("createdDate")]
    [JsonPropertyName("createdDate")]
    [AutoIndexed]
    public DateTime CreatedDate { get; private set; }

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    [AutoIndexed]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("type")]
    [JsonPropertyName("type")]
    [AutoIndexed]
    public string Type { get; set; } = default!;

    [BsonElement("name")]
    [JsonPropertyName("name")]
    [AutoIndexed]
    public string Name { get; set; } = default!;

    [BsonElement("state")]
    [JsonPropertyName("state")]
    [AutoIndexed]
    public string? State { get; set; } = default!;

    [BsonElement("startDate")]
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    [BsonElement("endDate")]
    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

    [BsonElement("source")]
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [BsonElement("destroyIdentityDocumentsFlag")]
    [JsonPropertyName("destroyIdentityDocumentsFlag")]
    public bool? DestroyIdentityDocumentsFlag { get; set; }

    [BsonElement("deleted")]
    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }

    [BsonElement("location")]
    [JsonPropertyName("location")]
    public LocationDocument? Location { get; set; }

    [BsonElement("identifiers")]
    [JsonPropertyName("identifiers")]
    public List<SiteIdentifierDocument> Identifiers { get; private set; } = [];

    [BsonElement("parties")]
    [JsonPropertyName("parties")]
    public List<SitePartyDocument> Parties { get; set; } = [];

    [BsonElement("species")]
    [JsonPropertyName("species")]
    public List<SpeciesSummaryDocument> Species { get; set; } = [];

    [BsonElement("marks")]
    [JsonPropertyName("marks")]
    public List<GroupMarkDocument> Marks { get; set; } = [];

    [BsonElement("siteActivities")]
    [JsonPropertyName("siteActivities")]
    public List<SiteActivityDocument> SiteActivities { get; set; } = [];

    [BsonElement("activities")]
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

        if (Species is not null && Species.Count > 0)
        {
            var species = Species
                .Select(s => new Species(
                    id: s.IdentifierId,
                    lastUpdatedDate: s.LastModifiedDate,
                    code: s.Code,
                    name: s.Name))
                .ToList();

            site.SetSpecies(species, LastUpdatedDate);
        }

        if (SiteActivities is not null && SiteActivities.Count > 0)
        {
            var activities = SiteActivities
                .Select(a => new SiteActivity(
                    id: a.IdentifierId,
                    activity: a.Activity,
                    description: a.Description,
                    startDate: a.StartDate,
                    endDate: a.EndDate,
                    lastUpdatedDate: a.LastUpdatedDate))
                .ToList();

            site.SetActivities(activities, LastUpdatedDate);
        }

        if (Marks is not null && Marks.Count > 0)
        {
            var groupMarks = Marks
                .Select(m => new GroupMark(
                    id: m.IdentifierId,
                    lastUpdatedDate: m.LastUpdatedDate,
                    mark: m.Mark,
                    startDate: m.StartDate,
                    endDate: m.EndDate,
                    species: m.Species is not null
                        ? new Species(
                            id: m.Species.IdentifierId,
                            lastUpdatedDate: m.Species.LastModifiedDate,
                            code: m.Species.Code,
                            name: m.Species.Name)
                        : null))
                .ToList();

            site.SetGroupMarks(groupMarks, LastUpdatedDate);
        }

        if (Parties is not null && Parties.Count > 0)
        {
            var siteParties = Parties
                .Select(p => new SiteParty(
                    id: p.IdentifierId,
                    createdDate: p.CreatedDate,
                    lastUpdatedDate: p.LastUpdatedDate,
                    partyId: p.PartyId,
                    title: p.Title,
                    firstName: p.FirstName,
                    lastName: p.LastName,
                    name: p.Name,
                    partyType: p.PartyType,
                    state: p.State,
                    correspondanceAddress: p.CorrespondanceAddress?.ToDomain(),
                    communication: p.Communication?.Select(c => c.ToDomain()),
                    partyRole: p.PartyRoles?.Select(r => r.ToDomain())))
                .ToList();

            site.SetSiteParties(siteParties, LastUpdatedDate);
        }

        return site;
    }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return AutoIndexed.GetIndexModels<SiteDocument>().Concat(
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("identifiers.identifier"),
                new CreateIndexOptions { Name = "idx_identifiers_identifier", Sparse = true })
        ]);
    }
}