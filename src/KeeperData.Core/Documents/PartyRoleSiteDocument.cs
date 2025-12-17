using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class PartyRoleSiteDocument : INestedEntity
{
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    [BsonElement("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [BsonElement("type")]
    [JsonPropertyName("type")]
    public PremisesTypeSummaryDocument? Type { get; set; }
    [JsonPropertyName("state")]
    public string? State { get; set; } = default!;

    [BsonElement("identifiers")]
    [JsonPropertyName("identifiers")]
    public List<SiteIdentifierDocument> Identifiers { get; set; } = [];

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }

    public static PartyRoleSiteDocument FromDomain(PartyRoleSite m) => new()
    {
        IdentifierId = m.Id,
        Name = m.Name,
        Type = m.Type != null ? PremisesTypeSummaryDocument.FromDomain(m.Type) : null,
        State = m.State,
        LastUpdatedDate = m.LastUpdatedDate,
        Identifiers = m.Identifiers?
            .Select(SiteIdentifierDocument.FromDomain)
            .ToList() ?? []
    };

    public PartyRoleSite ToDomain()
    {
        var partyRoleSite = new PartyRoleSite(
            IdentifierId,
            Name,
            Type?.ToDomain(),
            State,
            LastUpdatedDate
        );

        if (Identifiers?.Count > 0)
        {
            var domainIdentifiers = Identifiers.Select(i => i.ToDomain()).ToList();
            partyRoleSite.SetIdentifiers(domainIdentifiers);
        }

        return partyRoleSite;
    }
}