namespace KeeperData.Core.DeadLetter;

/// <summary>
/// Summary of a dead-letter queue redrive operation.
/// </summary>
public class RedriveSummary
{
    /// <summary>
    /// The number of messages successfully redriven to the source queue.
    /// </summary>
    public int MessagesRedriven { get; set; }

    /// <summary>
    /// The number of messages that failed to be redriven.
    /// </summary>
    public int MessagesFailed { get; set; }

    /// <summary>
    /// The number of duplicate messages encountered during redrive.
    /// </summary>
    public int MessagesDuplicated { get; set; }

    /// <summary>
    /// The approximate number of messages remaining in the dead-letter queue after redrive.
    /// </summary>
    public int MessagesRemainingApprox { get; set; }

    /// <summary>
    /// The correlation identifiers of the redriven messages.
    /// </summary>
    public List<string> CorrelationIds { get; set; } = [];

    /// <summary>
    /// The timestamp when the redrive operation started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// The timestamp when the redrive operation completed.
    /// </summary>
    public DateTime CompletedAt { get; set; }
}