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
    public int? LastUpdatedBatchId { get; set; }
    public DateTime LastUpdatedDate { get; set; }

    public string? Title { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Name { get; set; }
    public string? CustomerNumber { get; set; }
    public string? PartyType { get; set; }
    public string? State { get; set; }
    public bool Deleted { get; set; }

    public AddressDocument? CorrespondanceAddress { get; set; }
    public List<CommunicationDocument> Communication { get; set; } = [];
    public List<PartyRoleDocument> PartyRoles { get; set; } = [];

    public static PartyDocument FromDomain(Party m) => new()
    {
        Id = m.Id,
        LastUpdatedBatchId = m.LastUpdatedBatchId,
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
            LastUpdatedBatchId,
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
                Builders<BsonDocument>.IndexKeys.Ascending("LastName"),  //TODO casing
                new CreateIndexOptions { Name = "idx_lastName" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("CustomerNumber"),
                new CreateIndexOptions { Name = "idx_customerNumber" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("PartyType"),
                new CreateIndexOptions { Name = "idx_partyType" })
        ];
    }
}