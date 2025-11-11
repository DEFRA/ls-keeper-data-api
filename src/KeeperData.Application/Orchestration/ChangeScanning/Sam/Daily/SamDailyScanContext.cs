namespace KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily;

public class SamDailyScanContext
{
    public DateTime CurrentDateTime { get; init; }
    public int PageSize { get; init; }
    public EntityScanContext Holdings { get; init; } = new();
    public EntityScanContext Holders { get; init; } = new();
    public EntityScanContext Herds { get; init; } = new();
    public EntityScanContext Parties { get; init; } = new();
}
