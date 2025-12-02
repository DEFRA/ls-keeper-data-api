using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

/// <summary>
/// Composite key: HoldingIdentifier, Herdmark, ProductionUsageId, PartyId, RoleTypeId
/// </summary>
[CollectionName("siteGroupMarkRelationships")]
public record SiteGroupMarkRelationshipDocument : IEntity, IContainsIndexes
{
    [BsonId]
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public string? Id { get; set; }

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("partyId")]
    [JsonPropertyName("partyId")]
    [AutoIndexed]
    public string PartyId { get; set; } = string.Empty;

    [BsonElement("partyTypeId")]
    [JsonPropertyName("partyTypeId")]
    public string PartyTypeId { get; set; } = string.Empty;

    [BsonElement("herdmark")]
    [JsonPropertyName("herdmark")]
    [AutoIndexed]
    public string Herdmark { get; set; } = string.Empty;

    [BsonElement("countyParishHoldingHerd")]
    [JsonPropertyName("countyParishHoldingHerd")]
    public string CountyParishHoldingHerd { get; set; } = string.Empty;

    [BsonElement("holdingIdentifier")]
    [JsonPropertyName("holdingIdentifier")]
    [AutoIndexed]
    public string HoldingIdentifier { get; set; } = string.Empty;

    [BsonElement("holdingIdentifierType")]
    [JsonPropertyName("holdingIdentifierType")]
    [AutoIndexed]
    public string HoldingIdentifierType { get; set; } = string.Empty;

    [BsonElement("roleTypeId")]
    [JsonPropertyName("roleTypeId")]
    [AutoIndexed]
    public string? RoleTypeId { get; set; }

    [BsonElement("roleTypeName")]
    [JsonPropertyName("roleTypeName")]
    [AutoIndexed]
    public string? RoleTypeName { get; set; }

    [BsonElement("speciesTypeId")]
    [JsonPropertyName("speciesTypeId")]
    [AutoIndexed]
    public string? SpeciesTypeId { get; set; }

    [BsonElement("speciesTypeCode")]
    [JsonPropertyName("speciesTypeCode")]
    [AutoIndexed]
    public string? SpeciesTypeCode { get; set; }

    [BsonElement("productionUsageId")]
    [JsonPropertyName("productionUsageId")]
    [AutoIndexed]
    public string? ProductionUsageId { get; set; }

    [BsonElement("productionUsageCode")]
    [JsonPropertyName("productionUsageCode")]
    [AutoIndexed]
    public string? ProductionUsageCode { get; set; }

    [BsonElement("productionTypeId")]
    [JsonPropertyName("productionTypeId")]
    [AutoIndexed]
    public string? ProductionTypeId { get; set; }

    [BsonElement("productionTypeCode")]
    [JsonPropertyName("productionTypeCode")]
    [AutoIndexed]
    public string? ProductionTypeCode { get; set; }

    [BsonElement("diseaseType")]
    [JsonPropertyName("diseaseType")]
    public string? DiseaseType { get; set; }

    [BsonElement("interval")]
    [JsonPropertyName("interval")]
    public decimal? Interval { get; set; }

    [BsonElement("intervalUnitOfTime")]
    [JsonPropertyName("intervalUnitOfTime")]
    public string? IntervalUnitOfTime { get; set; }

    [BsonElement("groupMarkStartDate")]
    [JsonPropertyName("groupMarkStartDate")]
    public DateTime GroupMarkStartDate { get; set; } = default;

    [BsonElement("groupMarkEndDate")]
    [JsonPropertyName("groupMarkEndDate")]
    public DateTime? GroupMarkEndDate { get; set; }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return AutoIndexed.GetIndexModels<SiteGroupMarkRelationshipDocument>();
    }
}