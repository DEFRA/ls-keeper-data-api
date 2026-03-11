namespace KeeperData.Core.DeadLetter
{
    public class DeadLetterMessageDto
    {
        public string MessageId { get; set; } = string.Empty;
        public string? OriginalMessageId { get; set; }
        public string? FailureReason { get; set; }
        public string? FailureMessage { get; set; }
        public string? FailureTimestamp { get; set; }
        public string? ReceiveCount { get; set; }
        public string? CorrelationId { get; set; }
        public string? MessageType { get; set; }
        public string Body { get; set; } = string.Empty;
    }
}