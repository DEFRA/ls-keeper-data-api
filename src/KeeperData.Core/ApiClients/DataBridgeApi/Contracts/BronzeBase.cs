namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class BronzeBase
{
    public required int BATCH_ID { get; set; }
    public required string CHANGE_TYPE { get; set; }
    public bool? IsDeleted { get; set; }

    protected static List<string> SplitCommaSeparatedIds(string ids) =>
        string.IsNullOrWhiteSpace(ids)
            ? []
            : [.. ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    protected static string UnwrapCoalesced(string? value, string placeholder = "-") =>
        string.IsNullOrWhiteSpace(value) || value == placeholder ? string.Empty : value;
}