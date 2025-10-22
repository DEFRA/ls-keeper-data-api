using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class SpeciesDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public DateTime? LastUpdatedDate { get; set; }

    public static SpeciesDocument FromDomain(Species m) => new()
    {
        IdentifierId = m.Id,
        Code = m.Code,
        Name = m.Name,
        LastUpdatedDate = m.LastUpdatedDate
    };

    public Species ToDomain() => new(
        IdentifierId,
        Code ?? string.Empty,
        Name ?? string.Empty,
        LastUpdatedDate
    );
}