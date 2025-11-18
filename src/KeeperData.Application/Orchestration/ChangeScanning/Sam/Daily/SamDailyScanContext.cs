namespace KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily;

public class SamDailyScanContext
{
    public Guid ScanCorrelationId { get; init; } = Guid.NewGuid();
    public DateTime CurrentDateTime { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedSinceDateTime { get; init; }
    public int PageSize { get; init; } = 100;
    public EntityScanContext Holdings { get; init; } = new();
    public EntityScanContext Holders { get; init; } = new();
    public EntityScanContext Herds { get; init; } = new();
    public EntityScanContext Parties { get; init; } = new();
}