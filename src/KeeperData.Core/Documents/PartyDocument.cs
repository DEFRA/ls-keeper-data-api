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
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [BsonId]
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string Id { get; set; }

    [BsonElement("createdDate")]
    [JsonPropertyName("createdDate")]
    [JsonIgnore]
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// The timestamp of the last time the Party record was updated.
    /// </summary>
    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    [AutoIndexed]
    public DateTime LastUpdatedDate { get; set; }

    /// <summary>
    /// The title of the party (e.g. Mr, Mrs).
    /// </summary>
    /// <example>Mr</example>
    [BsonElement("title")]
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// The first name of the party.
    /// </summary>
    /// <example>John</example>
    [BsonElement("firstName")]
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    /// <summary>
    /// The last name of the party.
    /// </summary>
    /// <example>Doe</example>
    [BsonElement("lastName")]
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    /// <summary>
    /// The full name of the party.
    /// </summary>
    /// <example>John Doe</example>
    [BsonElement("name")]
    [JsonPropertyName("name")]
    [AutoIndexed]
    public string? Name { get; set; }

    /// <summary>
    /// This is the L or the C number provided from the SAM system.
    /// </summary>
    /// <example>C77473</example>
    [BsonElement("customerNumber")]
    [JsonPropertyName("customerNumber")]
    public string CustomerNumber { get; set; } = string.Empty;

    /// <summary>
    /// The type of party (e.g. Person, Organisation).
    /// </summary>
    /// <example>Person</example>
    [BsonElement("partyType")]
    [JsonPropertyName("partyType")]
    public string? PartyType { get; set; }

    /// <summary>
    /// The current state of the party.
    /// </summary>
    [BsonElement("state")]
    [JsonPropertyName("state")]
    [AutoIndexed]
    public string? State { get; set; }

    [BsonElement("deleted")]
    [JsonPropertyName("deleted")]
    [JsonIgnore]
    [AutoIndexed]
    public bool Deleted { get; set; }

    /// <summary>
    /// The communication details of the party.
    /// </summary>
    [BsonElement("communication")]
    [JsonPropertyName("communication")]
    public List<CommunicationDocument> Communication { get; set; } = [];

    /// <summary>
    /// The correspondence address of the party.
    /// </summary>
    [BsonElement("correspondanceAddress")]
    [JsonPropertyName("correspondanceAddress")]
    public AddressDocument? CorrespondanceAddress { get; set; }

    /// <summary>
    /// The roles assigned to the party, including site associations.
    /// </summary>
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

    public SiteParty ToSitePartyDomain(DateTime lastUpdatedDate)
    {
        return new SiteParty(
            id: Id,
            createdDate: CreatedDate,
            lastUpdatedDate: lastUpdatedDate,
            customerNumber: CustomerNumber,
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

            if (domainRolesById.TryGetValue(partyRole.IdentifierId, out var domainRole) && domainRole.Site != null)
                partyRole.Site = PartyRoleSiteDocument.FromDomain(domainRole.Site);
        }
    }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return Enumerable.Concat(
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("lastName"),
                new CreateIndexOptions { Name = "idxv2_lastName", Collation = IndexDefaults.CollationCaseInsensitive }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("firstName"),
                new CreateIndexOptions { Name = "idxv2_firstName", Collation = IndexDefaults.CollationCaseInsensitive }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("lastName").Ascending("firstName"),
                new CreateIndexOptions { Name = "idxv2_firstlastName", Collation = IndexDefaults.CollationCaseInsensitive }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("communication.email"),
                new CreateIndexOptions { Name = "idxv2_communication_email", Collation = IndexDefaults.CollationCaseInsensitive, Sparse = true }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("customerNumber"),
                new CreateIndexOptions
                {
                    Name = "uidx_customerNumber",
                    Unique = true,
                    Collation = IndexDefaults.CollationCaseInsensitive,
                    Sparse = true
                })
        ],
        AutoIndexedAttribute.GetIndexModels<PartyDocument>());
    }
}