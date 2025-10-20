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

    public static Dictionary<string, string> SamHoldingsByCph(string id)
    {
        return new Dictionary<string, string>
        {
            ["$filter"] = $"CPH eq '{id}'"
        };
    }

    public static Dictionary<string, string> SamHoldersByCph(string id)
    {
        return new Dictionary<string, string>
        {
            ["$filter"] = $"contains(CPHS, '{id}')"
        };
    }

    public static Dictionary<string, string> SamHerdsByCph(string id)
    {
        return new Dictionary<string, string>
        {
            ["$filter"] = $"CPHH eq '{id}'"
        };
    }

    public static Dictionary<string, string> SamPartyByPartyId(string id)
    {
        return new Dictionary<string, string>
        {
            ["$filter"] = $"PARTY_ID eq '{id}'"
        };
    }

    public static Dictionary<string, string> SamPartiesByPartyIds(IEnumerable<string> ids)
    {
        var filter = string.Join(" or ", ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => $"PARTY_ID eq '{id}'"));

        return new Dictionary<string, string>
        {
            ["$filter"] = filter
        };
    }
}