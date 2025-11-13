namespace KeeperData.Application.Orchestration.ChangeScanning.Cts.Daily;

public class CtsDailyScanContext
{
    public DateTime CurrentDateTime { get; init; }
    public int PageSize { get; init; }
    public EntityScanContext Holdings { get; init; } = new();
    public EntityScanContext Agents { get; init; } = new();
    public EntityScanContext Keepers { get; init; } = new();
}