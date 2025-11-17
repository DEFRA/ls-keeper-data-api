using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

[CollectionName("siteGroupMarkRelationships")]
public class SiteGroupMarkRelationshipDocument : IEntity, IContainsIndexes, IDeletableEntity
{
    [BsonId]
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }

    [JsonPropertyName("partyId")]
    public string PartyId { get; set; } = string.Empty;

    [JsonPropertyName("partyTypeId")]
    public string PartyTypeId { get; set; } = string.Empty;

    [JsonPropertyName("isHolder")]
    public bool IsHolder { get; set; }

    [JsonPropertyName("herdmark")]
    public string Herdmark { get; set; } = string.Empty;

    [JsonPropertyName("countyParishHoldingHerd")]
    public string CountyParishHoldingHerd { get; set; } = string.Empty;

    [JsonPropertyName("holdingIdentifier")]
    public string HoldingIdentifier { get; set; } = string.Empty;

    [JsonPropertyName("holdingIdentifierType")]
    public string HoldingIdentifierType { get; set; } = string.Empty;

    [JsonPropertyName("roleTypeId")]
    public string? RoleTypeId { get; set; }

    [JsonPropertyName("roleTypeName")]
    public string? RoleTypeName { get; set; }

    [JsonPropertyName("speciesTypeId")]
    public string? SpeciesTypeId { get; set; }

    [JsonPropertyName("speciesTypeCode")]
    public string? SpeciesTypeCode { get; set; }

    [JsonPropertyName("productionUsageId")]
    public string? ProductionUsageId { get; set; }

    [JsonPropertyName("productionUsageCode")]
    public string? ProductionUsageCode { get; set; }

    [JsonPropertyName("productionTypeId")]
    public string? ProductionTypeId { get; set; }

    [JsonPropertyName("productionTypeCode")]
    public string? ProductionTypeCode { get; set; }

    [JsonPropertyName("diseaseType")]
    public string? DiseaseType { get; set; }

    [JsonPropertyName("interval")]
    public decimal? Interval { get; set; }

    [JsonPropertyName("intervalUnitOfTime")]
    public string? IntervalUnitOfTime { get; set; }

    [JsonPropertyName("groupMarkStartDate")]
    public DateTime GroupMarkStartDate { get; set; } = default;

    [JsonPropertyName("groupMarkEndDate")]
    public DateTime? GroupMarkEndDate { get; set; }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("HoldingIdentifier"),
                new CreateIndexOptions { Name = "idx_holdingIdentifier" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("HoldingIdentifierType"),
                new CreateIndexOptions { Name = "idx_holdingIdentifierType" })
        ];
    }
}