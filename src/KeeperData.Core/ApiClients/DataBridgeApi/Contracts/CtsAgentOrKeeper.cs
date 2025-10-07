namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class CtsAgentOrKeeper : BronzeBase
{
    public string PAR_ID { get; set; } = string.Empty;

    public string? PAR_TITLE { get; set; }
    public string? PAR_INITIALS { get; set; }
    public string? PAR_SURNAME { get; set; }
}