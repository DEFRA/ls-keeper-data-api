namespace KeeperData.Core.Messaging.Contracts;

public class SnsEnvelope
{
    public string Type { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string TopicArn { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime? Timestamp { get; set; } = default;
    public Dictionary<string, SnsMessageAttribute>? MessageAttributes { get; set; }
}

public class SnsMessageAttribute
{
    public string Type { get; set; } = default!;
    public string Value { get; set; } = default!;
}

