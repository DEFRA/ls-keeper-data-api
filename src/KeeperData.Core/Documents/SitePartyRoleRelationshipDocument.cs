using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

/// <summary>
/// Composite key: HoldingIdentifier, CustomerNumber, RoleTypeId, SpeciesTypeId
/// </summary>
[CollectionName("sitePartyRoleRelationships")]
public record SitePartyRoleRelationshipDocument : IEntity, IContainsIndexes
{
    [BsonId]
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public string? Id { get; set; }

    [BsonElement("customerNumber")]
    [JsonPropertyName("customerNumber")]
    public string CustomerNumber { get; set; } = string.Empty;

    [BsonElement("partyTypeId")]
    [JsonPropertyName("partyTypeId")]
    public string PartyTypeId { get; set; } = string.Empty;

    [BsonElement("holdingIdentifier")]
    [JsonPropertyName("holdingIdentifier")]
    [AutoIndexed]
    public string HoldingIdentifier { get; set; } = string.Empty;

    [BsonElement("roleTypeId")]
    [JsonPropertyName("roleTypeId")]
    public string? RoleTypeId { get; set; }

    [BsonElement("roleTypeName")]
    [JsonPropertyName("roleTypeName")]
    public string? RoleTypeName { get; set; }

    [BsonElement("speciesTypeId")]
    [JsonPropertyName("speciesTypeId")]
    public string? SpeciesTypeId { get; set; }

    [BsonElement("speciesTypeCode")]
    [JsonPropertyName("speciesTypeCode")]
    public string? SpeciesTypeCode { get; set; }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return AutoIndexedAttribute.GetIndexModels<SitePartyRoleRelationshipDocument>();
    }
}