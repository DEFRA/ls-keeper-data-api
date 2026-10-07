using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

/// <summary>
/// A role assigned to a party, with species managed under that role.
/// </summary>
public class PartyRoleDocument : INestedEntity
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The role assigned to the party.
    /// </summary>
    [BsonElement("role")]
    [JsonPropertyName("role")]
    public required PartyRoleRoleDocument Role { get; set; }

    /// <summary>
    /// The list of species managed by the said Role assigned to the said Party.
    /// </summary>
    [BsonElement("speciesManagedByRole")]
    [JsonPropertyName("speciesManagedByRole")]
    public List<ManagedSpeciesDocument> SpeciesManagedByRole { get; set; } = [];

    /// <summary>
    /// The timestamp of the last time the Party Role record was updated.
    /// </summary>
    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }
}