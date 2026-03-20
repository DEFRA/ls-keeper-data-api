namespace KeeperData.Core.DeadLetter
{
    public class DeadLetterMessageDto
    {
        public string MessageId { get; set; } = string.Empty;
        public string? CorrelationId { get; set; }
        public string? MessageType { get; set; }
        public string Body { get; set; } = string.Empty;
        public string? SentTimestamp { get; set; }
        public string? ApproximateFirstReceiveTimestamp { get; set; }
        public string? Md5OfBody { get; set; }
    }
}