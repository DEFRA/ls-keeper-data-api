namespace KeeperData.Application.Orchestration.ChangeScanning.Cts.Daily;

public class CtsDailyScanContext
{
    public DateTime CurrentDateTime { get; init; }
    public DateTime? UpdatedSinceDateTime { get; init; }
    public int PageSize { get; init; }
    public Guid ScanCorrelationId { get; init; } = Guid.NewGuid();
    public DateTime CurrentDateTime { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedSinceDateTime { get; init; }
    public int PageSize { get; init; } = 100;
    public EntityScanContext Holdings { get; init; } = new();
    public EntityScanContext Agents { get; init; } = new();
    public EntityScanContext Keepers { get; init; } = new();
}