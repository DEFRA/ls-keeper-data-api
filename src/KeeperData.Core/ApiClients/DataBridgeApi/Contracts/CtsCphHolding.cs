namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class CtsCphHolding
{
    public string LID_FULL_IDENTIFIER { get; set; } = string.Empty;

    public string? LTY_LOC_TYPE { get; set; }
    public string? LOC_MAP_REFERENCE { get; set; }
    public string? LOC_EFFECTIVE_FROM { get; set; }
    public string? LOC_EFFECTIVE_TO { get; set; }

    public string? ADR_NAME { get; set; }
    public string? ADR_ADDRESS_2 { get; set; }
    public string? ADR_ADDRESS_3 { get; set; }
    public string? ADR_ADDRESS_4 { get; set; }
    public string? ADR_ADDRESS_5 { get; set; }
    public string? ADR_POST_CODE { get; set; }
}