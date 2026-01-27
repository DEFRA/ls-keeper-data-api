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
    [BsonElement("id")]
    public required string Id { get; set; }

    [BsonElement("createdDate")]
    [JsonPropertyName("createdDate")]
    [JsonIgnore]
    public DateTime CreatedDate { get; set; }

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    [AutoIndexed]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("type")]
    [JsonPropertyName("type")]
    public PremisesTypeSummaryDocument? Type { get; set; }

    [BsonElement("name")]
    [JsonPropertyName("name")]
    [AutoIndexed]
    public string Name { get; set; } = default!;

    [BsonElement("state")]
    [JsonPropertyName("state")]
    [AutoIndexed]
    public string? State { get; set; }

    [BsonElement("startDate")]
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    [BsonElement("endDate")]
    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

    [BsonElement("source")]
    [JsonPropertyName("source")]
    [AutoIndexed]
    public string? Source { get; set; }

    [BsonElement("destroyIdentityDocumentsFlag")]
    [JsonPropertyName("destroyIdentityDocumentsFlag")]
    public bool? DestroyIdentityDocumentsFlag { get; set; }

    [BsonElement("deleted")]
    [JsonPropertyName("deleted")]
    [JsonIgnore]
    [AutoIndexed]
    public bool Deleted { get; set; }

    [BsonElement("location")]
    [JsonPropertyName("location")]
    public LocationDocument? Location { get; set; }

    [BsonElement("identifiers")]
    [JsonPropertyName("identifiers")]
    public List<SiteIdentifierDocument> Identifiers { get; set; } = [];

    [BsonElement("parties")]
    [JsonPropertyName("parties")]
    public List<SitePartyDocument> Parties { get; set; } = [];

    [BsonElement("species")]
    [JsonPropertyName("species")]
    public List<SpeciesSummaryDocument> Species { get; set; } = [];

    [BsonElement("marks")]
    [JsonPropertyName("marks")]
    public List<GroupMarkDocument> Marks { get; set; } = [];

    [BsonElement("activities")]
    [JsonPropertyName("activities")]
    public List<SiteActivityDocument> Activities { get; set; } = [];

    public static SiteDocument FromDomain(Site m) => new()
    {
        Id = m.Id,
        CreatedDate = m.CreatedDate,
        LastUpdatedDate = m.LastUpdatedDate,
        Type = m.Type is not null ? PremisesTypeSummaryDocument.FromDomain(m.Type) : null,
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
        Activities = [.. m.Activities.Select(SiteActivityDocument.FromDomain)]
    };

    public Site ToDomain()
    {
        var site = new Site(
            Id,
            CreatedDate,
            LastUpdatedDate,
            Name,
            StartDate,
            EndDate,
            State,
            Source,
            DestroyIdentityDocumentsFlag,
            Deleted,
            Type?.ToDomain(),
            Location?.ToDomain()
        );

        foreach (var si in Identifiers)
        {
            site.SetSiteIdentifier(
                si.LastUpdatedDate,
                si.Identifier,
                si.Type.ToDomain(),
                si.IdentifierId,
                LastUpdatedDate);
        }

        if (Species is not null && Species.Count > 0)
        {
            var species = Species
                .Select(s => Domain.Shared.Species.Create(
                    id: s.IdentifierId,
                    lastUpdatedDate: s.LastModifiedDate,
                    code: s.Code,
                    name: s.Name))
                .ToList();

            site.SetSpecies(species, LastUpdatedDate);
        }

        if (Activities is not null && Activities.Count > 0)
        {
            var activities = Activities
                .Select(a => SiteActivity.Create(
                    id: a.IdentifierId,
                    type: a.Type.ToDomain(),
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
                    species: m.Species.Select(s => Domain.Shared.Species.Create(
                            id: s.IdentifierId,
                            lastUpdatedDate: s.LastModifiedDate,
                            code: s.Code,
                            name: s.Name))
                        ))
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
                    customerNumber: p.CustomerNumber,
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

            site.SetSiteParties(site.Id, siteParties, LastUpdatedDate);
        }

        return site;
    }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return AutoIndexedAttribute.GetIndexModels<SiteDocument>().Concat(
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("type.code"),
                new CreateIndexOptions { Name = "idxv2_type_code", Sparse = true }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("identifiers.identifier"),
                new CreateIndexOptions { Name = "idxv2_identifiers_identifier", Sparse = true }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("parties.id"),
                new CreateIndexOptions { Name = "idxv2_parties_id", Sparse = true }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("parties.customerNumber"),
                new CreateIndexOptions { Name = "idxv2_parties_customerNumber", Sparse = true })
        ]);
    }
}