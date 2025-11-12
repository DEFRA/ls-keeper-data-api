namespace KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;

public class CtsBulkScanContext
{
    public DateTime CurrentDateTime { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedSinceDateTime { get; init; }
    public int PageSize { get; init; } = 100;
    public EntityScanContext Holdings { get; init; } = new();
}
