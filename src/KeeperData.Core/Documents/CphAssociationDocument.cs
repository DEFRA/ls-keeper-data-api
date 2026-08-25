using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

/// <summary>
/// An association between a user account and a CPH, snapshotted from the normalised SAM read model.
/// </summary>
public class CphAssociationDocument : INestedEntity
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the party role which granted
    /// the association in the read model.
    /// </summary>
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The CPH number the user is associated with.
    /// </summary>
    /// <example>57/103/2335</example>
    [BsonElement("cphNumber")]
    [JsonPropertyName("cphNumber")]
    public required string CphNumber { get; set; }

    /// <summary>
    /// The role the user holds on the holding.
    /// </summary>
    /// <example>owner</example>
    [BsonElement("role")]
    [JsonPropertyName("role")]
    public required string Role { get; set; }

    /// <summary>
    /// The SAM identifier of the party which grants the association.
    /// </summary>
    [BsonElement("partyId")]
    [JsonPropertyName("partyId")]
    public string? PartyId { get; set; }

    /// <summary>
    /// The read model identifier of the holding the CPH number belongs to.
    /// </summary>
    [BsonElement("holdingId")]
    [JsonPropertyName("holdingId")]
    public string? HoldingId { get; set; }

    /// <summary>
    /// The name of the holding, where the source carries one.
    /// </summary>
    [BsonElement("holdingName")]
    [JsonPropertyName("holdingName")]
    public string? HoldingName { get; set; }
}
