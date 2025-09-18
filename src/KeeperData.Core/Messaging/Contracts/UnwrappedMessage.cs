namespace KeeperData.Core.Messaging.Contracts;

public class UnwrappedMessage
{
    public string MessageId { get; init; } = default!;
    public string CorrelationId { get; init; } = default!;
    public string Subject { get; init; } = "Default";
    public string Payload { get; init; } = default!;
    public Dictionary<string, string>? Attributes { get; init; }
}
