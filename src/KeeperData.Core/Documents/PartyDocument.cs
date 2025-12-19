using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Parties;
using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

[CollectionName("parties")]
public class PartyDocument : IEntity, IDeletableEntity, IContainsIndexes
{
    [BsonId]
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string Id { get; set; }

    [BsonElement("createdDate")]
    [JsonPropertyName("createdDate")]
    [JsonIgnore]
    [AutoIndexed]
    public DateTime CreatedDate { get; set; }

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    [AutoIndexed]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("title")]
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [BsonElement("firstName")]
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [BsonElement("lastName")]
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [BsonElement("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [BsonElement("customerNumber")]
    [JsonPropertyName("customerNumber")]
    [AutoIndexed]
    public string? CustomerNumber { get; set; }

    [BsonElement("partyType")]
    [JsonPropertyName("partyType")]
    [AutoIndexed]
    public string? PartyType { get; set; }

    [BsonElement("state")]
    [JsonPropertyName("state")]
    public string? State { get; set; }

    [BsonElement("deleted")]
    [JsonPropertyName("deleted")]
    [JsonIgnore]
    public bool Deleted { get; set; }

    [BsonElement("communication")]
    [JsonPropertyName("communication")]
    public List<CommunicationDocument> Communication { get; set; } = [];

    [BsonElement("correspondanceAddress")]
    [JsonPropertyName("correspondanceAddress")]
    public AddressDocument? CorrespondanceAddress { get; set; }

    [BsonElement("partyRoles")]
    [JsonPropertyName("partyRoles")]
    public List<PartyRoleWithSiteDocument> PartyRoles { get; set; } = [];

    public static PartyDocument FromDomain(Party domain)
    {
        var addressDoc = domain.Address is not null
            ? AddressDocument.FromDomain(domain.Address)
            : null;

        var communications = domain.Communications?.Select(CommunicationDocument.FromDomain).ToList() ?? [];
        var roles = domain.Roles?.Select(PartyRoleWithSiteDocument.FromDomain).ToList() ?? [];

        return new PartyDocument
        {
            Id = domain.Id,
            CreatedDate = domain.CreatedDate,
            LastUpdatedDate = domain.LastUpdatedDate,
            Title = domain.Title,
            FirstName = domain.FirstName,
            LastName = domain.LastName,
            Name = domain.Name,
            CustomerNumber = domain.CustomerNumber,
            PartyType = domain.PartyType,
            State = domain.State,
            Deleted = domain.Deleted,
            CorrespondanceAddress = addressDoc,
            Communication = communications,
            PartyRoles = roles
        };
    }

    public Party ToDomain()
    {
        var party = new Party(
            Id,
            CreatedDate,
            LastUpdatedDate,
            Title,
            FirstName,
            LastName,
            Name,
            CustomerNumber,
            PartyType,
            State,
            Deleted,
            CorrespondanceAddress?.ToDomain());

        foreach (var comm in Communication)
        {
            party.AddOrUpdatePrimaryCommunication(LastUpdatedDate, comm.ToDomain());
        }

        party.SetRoles(PartyRoles.Select(r => r.ToDomain()));

        return party;
    }

    public static PartyDocument FromDomain(SiteParty siteParty)
    {
        var addressDoc = siteParty.CorrespondanceAddress is not null
            ? AddressDocument.FromDomain(siteParty.CorrespondanceAddress)
            : null;

        var communications = siteParty.Communication?.Select(CommunicationDocument.FromDomain).ToList() ?? [];
        var roles = siteParty.PartyRoles?.Select(PartyRoleWithSiteDocument.FromDomain).ToList() ?? [];

        return new PartyDocument
        {
            Id = siteParty.Id,
            CreatedDate = siteParty.CreatedDate,
            LastUpdatedDate = siteParty.LastUpdatedDate,
            Title = siteParty.Title,
            FirstName = siteParty.FirstName,
            LastName = siteParty.LastName,
            Name = siteParty.Name,
            CustomerNumber = siteParty.CustomerNumber,
            PartyType = siteParty.PartyType,
            State = siteParty.State,
            Deleted = false,
            CorrespondanceAddress = addressDoc,
            Communication = communications,
            PartyRoles = roles
        };
    }

    public SiteParty ToSitePartyDomain(DateTime lastUpdatedDate)
    {
        return new SiteParty(
            id: Id,
            createdDate: CreatedDate,
            lastUpdatedDate: lastUpdatedDate,
            customerNumber: CustomerNumber ?? string.Empty,
            title: Title,
            firstName: FirstName,
            lastName: LastName,
            name: Name,
            partyType: PartyType,
            state: State,
            correspondanceAddress: CorrespondanceAddress?.ToDomain(),
            communication: Communication.Select(c => c.ToDomain()),
            partyRole: PartyRoles.Select(r => r.ToDomain()));
    }

    public void UpdatePartyRoleSitesFromDomain(Party domain)
    {
        if (domain?.Roles == null || PartyRoles == null)
            return;

        var domainRolesById = domain.Roles.ToDictionary(r => r.Id);

        foreach (var partyRole in PartyRoles)
        {
            if (partyRole == null) continue;

            if (domainRolesById.TryGetValue(partyRole.IdentifierId, out var domainRole))
            {
                if (domainRole.Site != null)
                {
                    partyRole.Site = PartyRoleSiteDocument.FromDomain(domainRole.Site);
                }
            }
        }
    }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return Enumerable.Concat(
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("lastName").Ascending("firstName"),
                new CreateIndexOptions { Name = "idxv2_firstlastName", Collation = IndexDefaults.CollationCaseInsensitive }),
        ],
        AutoIndexedAttribute.GetIndexModels<PartyDocument>());
    }
}