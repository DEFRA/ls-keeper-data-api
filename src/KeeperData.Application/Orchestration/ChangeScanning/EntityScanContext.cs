namespace KeeperData.Application.Orchestration.ChangeScanning;

public class EntityScanContext
{
    public int TotalCount { get; set; }
    public int CurrentTop { get; set; }
    public int CurrentSkip { get; set; }
    public int CurrentCount { get; set; }
    public bool ScanCompleted { get; set; }
}