using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

/// <summary>
/// Composite key: HoldingIdentifier, Herdmark, ProductionUsageId, CustomerNumber, RoleTypeId
/// </summary>
public record SiteGroupMarkRelationshipDocument : IEntity
{
    [BsonId]
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public string? Id { get; set; }

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("customerNumber")]
    [JsonPropertyName("customerNumber")]
    public string CustomerNumber { get; set; } = string.Empty;

    [BsonElement("partyTypeId")]
    [JsonPropertyName("partyTypeId")]
    public string PartyTypeId { get; set; } = string.Empty;

    [BsonElement("herdmark")]
    [JsonPropertyName("herdmark")]
    public string Herdmark { get; set; } = string.Empty;

    [BsonElement("countyParishHoldingHerd")]
    [JsonPropertyName("countyParishHoldingHerd")]
    public string CountyParishHoldingHerd { get; set; } = string.Empty;

    [BsonElement("holdingIdentifier")]
    [JsonPropertyName("holdingIdentifier")]
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

    [BsonElement("speciesTypeName")]
    [JsonPropertyName("speciesTypeName")]
    public string? SpeciesTypeName { get; set; }

    [BsonElement("productionUsageId")]
    [JsonPropertyName("productionUsageId")]
    public string? ProductionUsageId { get; set; }

    [BsonElement("productionUsageCode")]
    [JsonPropertyName("productionUsageCode")]
    public string? ProductionUsageCode { get; set; }

    [BsonElement("productionTypeId")]
    [JsonPropertyName("productionTypeId")]
    public string? ProductionTypeId { get; set; }

    [BsonElement("productionTypeCode")]
    [JsonPropertyName("productionTypeCode")]
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
}