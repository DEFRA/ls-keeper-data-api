using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Silver;

public class GroupMarkDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }

    [JsonPropertyName("groupMark")]
    [BsonElement("groupMark")]
    public string GroupMark { get; set; } = string.Empty;

    [JsonPropertyName("countyParishHoldingNumber")]
    [BsonElement("countyParishHoldingNumber")]
    public string CountyParishHoldingNumber { get; set; } = string.Empty;

    [JsonPropertyName("groupMarkStartDate")]
    [BsonElement("groupMarkStartDate")]
    public DateTime GroupMarkStartDate { get; set; } = default;

    [JsonPropertyName("groupMarkEndDate")]
    [BsonElement("groupMarkEndDate")]
    public DateTime? GroupMarkEndDate { get; set; }

    [JsonPropertyName("speciesTypeId")]
    [BsonElement("speciesTypeId")]
    public string? SpeciesTypeId { get; set; } // LOV Lookup / Internal Id

    [JsonPropertyName("speciesTypeCode")]
    [BsonElement("speciesTypeCode")]
    public string? SpeciesTypeCode { get; set; }

    [JsonPropertyName("productionUsageId")]
    [BsonElement("productionUsageId")]
    public string? ProductionUsageId { get; set; } // LOV Lookup / Internal Id

    [JsonPropertyName("productionUsageCode")]
    [BsonElement("productionUsageCode")]
    public string? ProductionUsageCode { get; set; }

    [JsonPropertyName("productionTypeId")]
    [BsonElement("productionTypeId")]
    public string? ProductionTypeId { get; set; } // LOV Lookup / Internal Id

    [JsonPropertyName("productionTypeCode")]
    [BsonElement("productionTypeCode")]
    public string? ProductionTypeCode { get; set; }

    [JsonPropertyName("tbTestingIntervalId")]
    [BsonElement("tbTestingIntervalId")]
    public string? TbTestingIntervalId { get; set; }
}