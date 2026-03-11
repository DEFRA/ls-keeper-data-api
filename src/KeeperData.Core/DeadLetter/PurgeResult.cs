namespace KeeperData.Core.DeadLetter;

public class PurgeResult
{
    public bool Purged { get; set; }
    public int ApproximateMessagesPurged { get; set; }
    public DateTime PurgedAt { get; set; }
}