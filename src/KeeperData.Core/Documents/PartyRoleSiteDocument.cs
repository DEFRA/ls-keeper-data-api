using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

/// <summary>
/// A lightweight site summary embedded within a party's role.
/// </summary>
public class PartyRoleSiteDocument : INestedEntity
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The name of the site.
    /// </summary>
    [BsonElement("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The type of site.
    /// </summary>
    [BsonElement("type")]
    [JsonPropertyName("type")]
    public SiteTypeSummaryDocument? Type { get; set; }

    /// <summary>
    /// The current state of the site.
    /// </summary>
    [BsonElement("state")]
    [JsonPropertyName("state")]
    public string? State { get; set; } = default!;

    /// <summary>
    /// The identifiers associated with this site.
    /// </summary>
    [BsonElement("identifiers")]
    [JsonPropertyName("identifiers")]
    public List<SiteIdentifierDocument> Identifiers { get; set; } = [];

    /// <summary>
    /// The timestamp of the last time the PartyRoleSite record was updated.
    /// </summary>
    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }

    public static PartyRoleSiteDocument FromDomain(PartyRoleSite m) => new()
    {
        IdentifierId = m.Id,
        Name = m.Name,
        Type = m.Type != null ? SiteTypeSummaryDocument.FromDomain(m.Type) : null,
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