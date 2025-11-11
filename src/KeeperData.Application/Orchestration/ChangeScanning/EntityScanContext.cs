namespace KeeperData.Application.Orchestration.ChangeScanning;

public class EntityScanContext
{
    public int TotalCount { get; init; }
    public int CurrentTop { get; init; }
    public int CurrentSkip { get; init; }
    public int CurrentCount { get; init; }
    public bool ScanCompleted { get; init; }
}
