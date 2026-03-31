namespace KeeperData.Core.DeadLetter;

/// <summary>
/// Statistics for a dead-letter queue.
/// </summary>
public class QueueStats
{
    /// <summary>
    /// The URL of the dead-letter queue.
    /// </summary>
    public string QueueUrl { get; set; } = string.Empty;

    /// <summary>
    /// The approximate number of messages available for retrieval from the queue.
    /// </summary>
    public int ApproximateMessageCount { get; set; }

    /// <summary>
    /// The approximate number of messages that are in flight (sent to a consumer but not yet acknowledged).
    /// </summary>
    public int ApproximateMessagesNotVisible { get; set; }

    /// <summary>
    /// The approximate number of messages that are delayed and not yet available for reading.
    /// </summary>
    public int ApproximateMessagesDelayed { get; set; }

    /// <summary>
    /// The timestamp when the queue statistics were retrieved.
    /// </summary>
    public DateTime CheckedAt { get; set; }
}