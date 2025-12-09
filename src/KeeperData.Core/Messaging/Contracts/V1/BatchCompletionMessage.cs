namespace KeeperData.Core.Messaging.Contracts.V1;

public class BatchCompletionMessage : MessageType
{
    public string? ScanCorrelationId { get; set; }
}