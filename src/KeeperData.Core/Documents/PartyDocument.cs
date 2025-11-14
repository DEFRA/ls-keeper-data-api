using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Parties;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

[CollectionName("parties")]
public class PartyDocument : IEntity, IDeletableEntity, IContainsIndexes
{
    [BsonId]
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; private set; }

    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("customerNumber")]
    public string? CustomerNumber { get; set; }

    [JsonPropertyName("partyType")]
    public string? PartyType { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }

    [JsonPropertyName("communication")]
    public List<CommunicationDocument> Communication { get; set; } = [];

    [JsonPropertyName("correspondanceAddress")]
    public AddressDocument? CorrespondanceAddress { get; set; }

    [JsonPropertyName("partyRoles")]
    public List<PartyRoleDocument> PartyRoles { get; set; } = [];

    public static PartyDocument FromDomain(Party m) => new()
    {
        Id = m.Id,
        CreatedDate = m.CreatedDate,
        LastUpdatedDate = m.LastUpdatedDate,
        Title = m.Title,
        FirstName = m.FirstName,
        LastName = m.LastName,
        Name = m.Name,
        CustomerNumber = m.CustomerNumber,
        PartyType = m.PartyType,
        State = m.State,
        Deleted = m.Deleted,
        CorrespondanceAddress = m.Address is not null
            ? AddressDocument.FromDomain(m.Address)
            : null,
        Communication = [.. m.Communications.Select(CommunicationDocument.FromDomain)],
        PartyRoles = [.. m.Roles.Select(PartyRoleDocument.FromDomain)]
    };

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

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("Name"),
                new CreateIndexOptions { Name = "idx_name" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("FirstName"),
                new CreateIndexOptions { Name = "idx_firstName" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("LastName"),
                new CreateIndexOptions { Name = "idx_lastName" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("CustomerNumber"),
                new CreateIndexOptions { Name = "idx_customerNumber" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("PartyType"),
                new CreateIndexOptions { Name = "idx_partyType" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("CreatedDate"),
                new CreateIndexOptions { Name = "idx_createdDate" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("LastUpdatedDate"),
                new CreateIndexOptions { Name = "idx_lastUpdatedDate" }),
        ];
    }
}