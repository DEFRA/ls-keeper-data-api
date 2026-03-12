namespace KeeperData.Api.Tests.Component;

public static class TestConstants
{
    public const string DataBridgeApiBaseUrl = "http://localhost:5560";

    public const string HealthCheckEndpoint = "/health";

    public const string ImportStartCtsBulkScanEndpoint = "api/import/startCtsBulkScan";
    public const string ImportStartSamBulkScanEndpoint = "api/import/startSamBulkScan";
    public const string ImportStartCtsDailyScanEndpoint = "api/import/startCtsDailyScan";
    public const string ImportStartSamDailyScanEndpoint = "api/import/startSamDailyScan";

    public const string AdminDlqCountEndpoint = "api/admin/queues/deadletter/count";
    public const string AdminDlqMessagesEndpoint = "api/admin/queues/deadletter/messages";
    public const string AdminDlqRedriveEndpoint = "api/admin/queues/deadletter/redrive";
    public const string AdminDlqPurgeEndpoint = "api/admin/queues/deadletter/purge";
}