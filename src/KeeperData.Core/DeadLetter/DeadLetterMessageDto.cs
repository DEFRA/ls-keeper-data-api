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
        public string? CorrelationId { get; set; }

        /// <summary>
        /// The type of the original message.
        /// </summary>
        public string? MessageType { get; set; }

        /// <summary>
        /// The body content of the dead-letter message.
        /// </summary>
        public string Body { get; set; } = string.Empty;
        public string? SentTimestamp { get; set; }
        public string? ApproximateFirstReceiveTimestamp { get; set; }
        public string? Md5OfBody { get; set; }
    }
}