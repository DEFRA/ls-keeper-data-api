namespace KeeperData.Core.ApiClients.DataBridgeApi;

public static class DataBridgeQueries
{
    public static Dictionary<string, string> CtsHoldingsByLidFullIdentifier(string id)
    {
        return new Dictionary<string, string>
        {
            ["$filter"] = $"LID_FULL_IDENTIFIER eq '{id}'"
        };
    }

    public static Dictionary<string, string> CtsAgentsByLidFullIdentifier(string id)
    {
        return new Dictionary<string, string>
        {
            ["$filter"] = $"LID_FULL_IDENTIFIER eq '{id}'"
        };
    }

    public static Dictionary<string, string> CtsKeepersByLidFullIdentifier(string id)
    {
        return new Dictionary<string, string>
        {
            ["$filter"] = $"LID_FULL_IDENTIFIER eq '{id}'"
        };
    }
}