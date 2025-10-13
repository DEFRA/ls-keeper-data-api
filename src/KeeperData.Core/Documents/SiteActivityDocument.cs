using KeeperData.Core.Domain.Sites; // Add this using
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class SiteActivityDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }
    public string? Activity { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? LastUpdatedDate { get; set; }

    public static SiteActivityDocument FromDomain(SiteActivity m) => new()
    {
        IdentifierId = m.Id,
        Activity = m.Activity,
        Description = m.Description,
        StartDate = m.StartDate,
        EndDate = m.EndDate,
        LastUpdatedDate = m.LastUpdatedDate
    };

    public SiteActivity ToDomain() => new(
        IdentifierId,
        Activity ?? string.Empty,
        Description,
        StartDate ?? DateTime.MinValue,
        EndDate,
        LastUpdatedDate
    );
}