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
    public string PartyId { get; set; } = string.Empty;

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

    [BsonElement("holdingIdentifierType")]
    [JsonPropertyName("holdingIdentifierType")]
    public string HoldingIdentifierType { get; set; } = string.Empty;

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

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("HoldingIdentifier"),
                new CreateIndexOptions { Name = "idx_holdingIdentifier" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("HoldingIdentifierType"),
                new CreateIndexOptions { Name = "idx_holdingIdentifierType" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("PartyId"),
                new CreateIndexOptions { Name = "idx_partyId" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("Herdmark"),
                new CreateIndexOptions { Name = "idx_herdmark" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("RoleTypeId"),
                new CreateIndexOptions { Name = "idx_roleTypeId" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("RoleTypeName"),
                new CreateIndexOptions { Name = "idx_roleTypeName" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("SpeciesTypeId"),
                new CreateIndexOptions { Name = "idx_speciesTypeId" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("SpeciesTypeCode"),
                new CreateIndexOptions { Name = "idx_speciesTypeCode" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("ProductionUsageId"),
                new CreateIndexOptions { Name = "idx_productionUsageId" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("ProductionUsageCode"),
                new CreateIndexOptions { Name = "idx_productionUsageCode" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("ProductionTypeId"),
                new CreateIndexOptions { Name = "idx_productionTypeId" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("ProductionTypeCode"),
                new CreateIndexOptions { Name = "idx_productionTypeCode" })
        ];
    }
}