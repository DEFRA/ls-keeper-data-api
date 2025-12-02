using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class SamScanHolderIdentifier
{
    [JsonPropertyName("PARTY_ID")]
    public string PARTY_ID { get; set; } = string.Empty;

    /// <summary>
    /// CLOB (comma separated list of CPH)
    /// </summary>
    [JsonPropertyName("CPHS")]
    public string? CPHS { get; set; }

    public List<string> CphList => SplitCommaSeparatedIds(CPHS ?? string.Empty);

    protected static List<string> SplitCommaSeparatedIds(string ids) =>
        string.IsNullOrWhiteSpace(ids)
            ? []
            : [.. ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
}