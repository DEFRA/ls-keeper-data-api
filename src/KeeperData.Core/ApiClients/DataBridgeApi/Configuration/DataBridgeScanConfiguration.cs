namespace KeeperData.Core.ApiClients.DataBridgeApi.Configuration;

public class DataBridgeScanConfiguration
{
    public int QueryPageSize { get; set; } = 100;
    public int DelayBetweenQueriesSeconds { get; set; }
    public int LimitScanTotalBatchSize { get; set; }
    public int DailyScanIncludeChangesWithinTotalHours { get; set; }
}