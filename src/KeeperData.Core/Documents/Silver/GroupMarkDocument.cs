using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Silver;

public class GroupMarkDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }

    public string GroupMark { get; set; } = string.Empty;

    public DateTime GroupMarkStartDate { get; set; } = default;
    public DateTime? GroupMarkEndDate { get; set; }

    public string? SpeciesTypeId { get; set; }
    public string? SpeciesTypeCode { get; set; }

    public string? ProductionUsageId { get; set; }
    public string? ProductionUsageCode { get; set; }

    public string? TbTestingIntervalId { get; set; }
}
