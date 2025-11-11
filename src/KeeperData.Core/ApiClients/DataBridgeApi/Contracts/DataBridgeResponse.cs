using KeeperData.Core.ApiClients.DataBridgeApi.Converters;
using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class DataBridgeResponse<T>
{
    [JsonPropertyName("collectionName")]
    public required string CollectionName { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("skip")]
    public int Skip { get; set; }

    [JsonPropertyName("top")]
    public int Top { get; set; }

    [JsonPropertyName("filter")]
    public string? Filter { get; set; }

    [JsonPropertyName("orderBy")]
    public string? OrderBy { get; set; }

    [JsonPropertyName("executedAtUtc")]
    [JsonConverter(typeof(SafeNullableDateTimeConverter))]
    public DateTime? ExecutedAtUtc { get; set; }

    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = [];

}