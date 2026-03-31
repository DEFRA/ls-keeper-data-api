using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

/// <summary>
/// A lightweight species reference embedded within site and mark records.
/// </summary>
public class SpeciesSummaryDocument : INestedEntity
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the reference object.
    /// </summary>
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The species code.
    /// </summary>
    /// <example>CTT</example>
    [BsonElement("code")]
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    /// <summary>
    /// The species name.
    /// </summary>
    /// <example>Cattle</example>
    [BsonElement("name")]
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The timestamp of the last time the Species record was updated.
    /// </summary>
    [BsonElement("lastModifiedDate")]
    [JsonPropertyName("lastModifiedDate")]
    public DateTime? LastModifiedDate { get; set; }

    public static SpeciesSummaryDocument FromDomain(Species m) => new()
    {
        IdentifierId = m.Id,
        Code = m.Code,
        Name = m.Name,
        LastModifiedDate = m.LastUpdatedDate
    };

    public Species ToDomain() => new(
        id: IdentifierId,
        code: Code,
        name: Name,
        lastUpdatedDate: LastModifiedDate);
}