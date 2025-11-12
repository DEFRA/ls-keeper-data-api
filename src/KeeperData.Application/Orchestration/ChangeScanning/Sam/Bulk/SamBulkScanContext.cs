namespace KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk;

public class SamBulkScanContext
{
    public DateTime CurrentDateTime { get; init; }
    public int PageSize { get; init; }
    public EntityScanContext Holdings { get; init; } = new();
    public EntityScanContext Holders { get; init; } = new();
}
