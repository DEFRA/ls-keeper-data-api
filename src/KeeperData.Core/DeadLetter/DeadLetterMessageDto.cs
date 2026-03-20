namespace KeeperData.Core.DeadLetter
{
    /// <summary>
    /// A message retrieved from a dead-letter queue.
    /// </summary>
    public class DeadLetterMessageDto
    {
        /// <summary>
        /// The unique identifier of the dead-letter message.
        /// </summary>
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// The identifier of the original message that failed processing.
        /// </summary>
        public string? OriginalMessageId { get; set; }

        /// <summary>
        /// The reason the message was moved to the dead-letter queue.
        /// </summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// The failure message or exception detail.
        /// </summary>
        public string? FailureMessage { get; set; }

        /// <summary>
        /// The timestamp when the failure occurred.
        /// </summary>
        public string? FailureTimestamp { get; set; }

        /// <summary>
        /// The number of times this message has been received.
        /// </summary>
        public string? ReceiveCount { get; set; }

        /// <summary>
        /// The correlation identifier of the original message.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// The type of the original message.
        /// </summary>
        public string? MessageType { get; set; }

        /// <summary>
        /// The body content of the dead-letter message.
        /// </summary>
        public string Body { get; set; } = string.Empty;
    }
}