using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Silver;

/// <summary>
/// Composite key: CountyParishHoldingHerd, ProductionUsageCode, Herdmark
/// </summary>
[CollectionName("samHerds")]
public class SamHerdDocument : IEntity, IContainsIndexes, IDeletableEntity
{
    [BsonId]
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public string? Id { get; set; }

    [JsonPropertyName("lastUpdatedBatchId")]
    [BsonElement("lastUpdatedBatchId")]
    public int? LastUpdatedBatchId { get; set; }

    [BsonElement("createdDate")]
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }

    [JsonPropertyName("lastUpdatedDate")]
    [BsonElement("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [JsonPropertyName("deleted")]
    [BsonElement("deleted")]
    public bool Deleted { get; set; }

    [JsonPropertyName("herdmark")]
    [BsonElement("herdmark")]
    public string Herdmark { get; set; } = string.Empty;

    [JsonPropertyName("countyParishHoldingHerd")]
    [BsonElement("countyParishHoldingHerd")]
    public string CountyParishHoldingHerd { get; set; } = string.Empty;

    [JsonPropertyName("countyParishHoldingNumber")]
    [BsonElement("countyParishHoldingNumber")]
    public string CountyParishHoldingNumber { get; set; } = string.Empty;

    [JsonPropertyName("speciesTypeId")]
    [BsonElement("speciesTypeId")]
    public string? SpeciesTypeId { get; set; } // LOV Lookup / Internal Id

    [JsonPropertyName("speciesTypeCode")]
    [BsonElement("speciesTypeCode")]
    public string? SpeciesTypeCode { get; set; }

    [BsonElement("speciesTypeName")]
    [JsonPropertyName("speciesTypeName")]
    public string? SpeciesTypeName { get; set; }

    [JsonPropertyName("productionUsageId")]
    [BsonElement("productionUsageId")]
    public string? ProductionUsageId { get; set; } // LOV Lookup / Internal Id

    [JsonPropertyName("productionUsageCode")]
    [BsonElement("productionUsageCode")]
    public string? ProductionUsageCode { get; set; }

    [JsonPropertyName("animalPurposeCode")]
    [BsonElement("animalPurposeCode")]
    public string? AnimalPurposeCode { get; set; } // Original including sub type e.g. CTT-BEEF-ADLR

    [JsonPropertyName("productionTypeId")]
    [BsonElement("productionTypeId")]
    public string? ProductionTypeId { get; set; } // LOV Lookup / Internal Id

    [JsonPropertyName("productionTypeCode")]
    [BsonElement("productionTypeCode")]
    public string? ProductionTypeCode { get; set; }

    [JsonPropertyName("diseaseType")]
    [BsonElement("diseaseType")]
    public string? DiseaseType { get; set; }

    [JsonPropertyName("interval")]
    [BsonElement("interval")]
    public decimal? Interval { get; set; }

    [JsonPropertyName("intervalUnitOfTime")]
    [BsonElement("intervalUnitOfTime")]
    public string? IntervalUnitOfTime { get; set; }

    [JsonPropertyName("movementRestrictionReasonCode")]
    [BsonElement("movementRestrictionReasonCode")]
    public string? MovementRestrictionReasonCode { get; set; }

    [JsonPropertyName("groupMarkStartDate")]
    [BsonElement("groupMarkStartDate")]
    public DateTime GroupMarkStartDate { get; set; } = default;

    [JsonPropertyName("groupMarkEndDate")]
    [BsonElement("groupMarkEndDate")]
    public DateTime? GroupMarkEndDate { get; set; }

    [JsonPropertyName("keeperPartyIdList")]
    [BsonElement("keeperPartyIdList")]
    public List<string> KeeperPartyIdList { get; set; } = [];

    [JsonPropertyName("ownerPartyIdList")]
    [BsonElement("ownerPartyIdList")]
    public List<string> OwnerPartyIdList { get; set; } = [];

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("Herdmark"),
                new CreateIndexOptions { Name = "idx_herdmark" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("CountyParishHoldingNumber"),
                new CreateIndexOptions { Name = "idx_countyParishHoldingNumber" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("ProductionUsageCode"),
                new CreateIndexOptions { Name = "idx_productionUsageCode" })
        ];
    }
}