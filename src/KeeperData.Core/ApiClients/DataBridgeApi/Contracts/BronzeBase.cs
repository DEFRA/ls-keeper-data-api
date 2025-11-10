using KeeperData.Core.ApiClients.DataBridgeApi.Converters;
using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class BronzeBase
{
    [JsonPropertyName("BATCH_ID")]
    [JsonConverter(typeof(SafeNullableIntConverter))]
    public int? BATCH_ID { get; set; }

    [JsonPropertyName("CHANGE_TYPE")]
    public string CHANGE_TYPE { get; set; } = string.Empty;

    [JsonPropertyName("IsDeleted")]
    public bool? IsDeleted { get; set; }

    protected static List<string> SplitCommaSeparatedIds(string ids) =>
        string.IsNullOrWhiteSpace(ids)
            ? []
            : [.. ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    protected static string UnwrapCoalesced(string? value, string placeholder = "-") =>
        string.IsNullOrWhiteSpace(value) || value == placeholder ? string.Empty : value;
}