namespace KeeperData.Api.Tests.Component;

public static class TestConstants
{
    public const string DataBridgeApiBaseUrl = "http://localhost:5560";

    public const string HealthCheckEndpoint = "/health";

    public const string ImportStartCtsScanEndpoint = "api/import/startCtsScan";
    public const string ImportStartSamScanEndpoint = "api/import/startSamScan";

    public const string AdminDlqCountEndpoint = "api/admin/queues/deadletter/count";
    public const string AdminDlqPeekEndpoint = "api/admin/queues/deadletter/peek";
    public const string AdminDlqRedriveEndpoint = "api/admin/queues/deadletter/redrive";
    public const string AdminDlqPurgeEndpoint = "api/admin/queues/deadletter/purge";
}