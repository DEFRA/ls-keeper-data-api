namespace KeeperData.Core.DeadLetter;

/// <summary>
/// The result of a dead-letter queue purge operation.
/// </summary>
public class PurgeResult
{
    /// <summary>
    /// Whether the purge operation was successful.
    /// </summary>
    public bool Purged { get; set; }

    /// <summary>
    /// The approximate number of messages that were purged.
    /// </summary>
    public int ApproximateMessagesPurged { get; set; }

    /// <summary>
    /// The timestamp when the purge operation was performed.
    /// </summary>
    public DateTime PurgedAt { get; set; }
}