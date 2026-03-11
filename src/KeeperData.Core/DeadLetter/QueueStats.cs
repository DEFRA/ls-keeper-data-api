namespace KeeperData.Core.DeadLetter;

public class QueueStats
{
    public string QueueUrl { get; set; } = string.Empty;
    public int ApproximateMessageCount { get; set; }
    public int ApproximateMessagesNotVisible { get; set; }
    public int ApproximateMessagesDelayed { get; set; }
    public DateTime CheckedAt { get; set; }
}