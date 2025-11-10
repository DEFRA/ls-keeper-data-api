namespace KeeperData.Core.ApiClients.DataBridgeApi;

public static class DataBridgeApiRoutes
{
    public const string GetCtsHoldings = "ls-keeper-data-bridge-backend/api/query/cts_cph_holding";
    public const string GetCtsAgents = "ls-keeper-data-bridge-backend/api/query/cts_agent";
    public const string GetCtsKeepers = "ls-keeper-data-bridge-backend/api/query/cts_keeper";

    public const string GetSamHoldings = "ls-keeper-data-bridge-backend/api/query/sam_cph_holdings";
    public const string GetSamHolders = "ls-keeper-data-bridge-backend/api/query/sam_cph_holder";
    public const string GetSamParties = "ls-keeper-data-bridge-backend/api/query/sam_party";
    public const string GetSamHerds = "ls-keeper-data-bridge-backend/api/query/sam_herd";
}
