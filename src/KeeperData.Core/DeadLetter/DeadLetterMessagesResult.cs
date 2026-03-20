namespace KeeperData.Core.DeadLetter;

/// <summary>
/// The result of retrieving messages from a dead-letter queue.
/// </summary>
public class DeadLetterMessagesResult
{
    /// <summary>
    /// The list of dead-letter messages retrieved.
    /// </summary>
    public List<DeadLetterMessageDto> Messages { get; set; } = new();

    /// <summary>
    /// The approximate total number of messages remaining in the queue.
    /// </summary>
    public int TotalApproximateCount { get; set; }

    /// <summary>
    /// The timestamp when the messages were retrieved.
    /// </summary>
    public DateTime CheckedAt { get; set; }
}