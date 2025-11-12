namespace KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk;

public class SamBulkScanContext
{
    public DateTime CurrentDateTime { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedSinceDateTime { get; init; }
    public int PageSize { get; init; } = 100;
    public EntityScanContext Holders { get; init; } = new();
    public EntityScanContext Holdings { get; init; } = new();
}
