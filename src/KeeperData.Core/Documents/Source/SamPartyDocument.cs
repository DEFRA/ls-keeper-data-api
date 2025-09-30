using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KeeperData.Core.Documents.Source;

[CollectionName("samParties")]
public class SamPartyDocument : IEntity, IContainsIndexes
{
    public string Id { get; set; } = string.Empty;
    public string PartyId { get; set; } = string.Empty;
    public string? LandlineNumber { get; set; }
    public string? MobileNumber { get; set; }
    public string? EmailAddress { get; set; }
    public string? PartyFirstName { get; set; }
    public string? PartyFullName { get; set; }
    public string? PartyLastName { get; set; }
    public string? PartyTitleTypeIdentifier { get; set; }
    public AddressDocument? Address { get; set; }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("PartyId"),
                new CreateIndexOptions { Name = "idx_partyId" })
        ];
    }
}
