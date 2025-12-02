using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Silver;

/// <summary>
/// Composite key: CountyParishHoldingNumber, LocationName, SpeciesTypeCode, SecondaryCph
/// </summary>
[CollectionName("samHoldings")]
public class SamHoldingDocument : BaseHoldingDocument, IEntity, IDeletableEntity, IContainsIndexes
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

    [JsonPropertyName("cphRelationshipType")]
    [BsonElement("cphRelationshipType")]
    public string? CphRelationshipType { get; set; }

    [JsonPropertyName("secondaryCph")]
    [BsonElement("secondaryCph")]
    [AutoIndexed]
    public string? SecondaryCph { get; set; }

    [JsonPropertyName("diseaseType")]
    [BsonElement("diseaseType")]
    public string? DiseaseType { get; set; }

    [JsonPropertyName("interval")]
    [BsonElement("interval")]
    public decimal? Interval { get; set; }

    [JsonPropertyName("intervalUnitOfTime")]
    [BsonElement("intervalUnitOfTime")]
    public string? IntervalUnitOfTime { get; set; }

    [JsonPropertyName("premiseSubActivityTypeCode")]
    [BsonElement("premiseSubActivityTypeCode")]
    public string? PremiseSubActivityTypeCode { get; set; }

    [JsonPropertyName("movementRestrictionReasonCode")]
    [BsonElement("movementRestrictionReasonCode")]
    public string? MovementRestrictionReasonCode { get; set; }

    [JsonPropertyName("speciesTypeCode")]
    [BsonElement("speciesTypeCode")]
    [AutoIndexed]
    public string? SpeciesTypeCode { get; set; }

    [JsonPropertyName("productionUsageCodeList")]
    [BsonElement("productionUsageCodeList")]
    public List<string> ProductionUsageCodeList { get; set; } = [];

    public bool IsActive => HoldingStatus == HoldingStatusType.Active.ToString();

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return AutoIndexed.GetIndexModels<SamHoldingDocument>();
    }
}