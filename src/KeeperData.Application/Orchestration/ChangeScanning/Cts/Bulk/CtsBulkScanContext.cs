namespace KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;

public class CtsBulkScanContext
{
    public DateTime CurrentDateTime { get; init; }
    public int PageSize { get; init; }
    public EntityScanContext Holdings { get; init; } = new();
    public EntityScanContext Agents { get; init; } = new();
    public EntityScanContext Keepers { get; init; } = new();
}
