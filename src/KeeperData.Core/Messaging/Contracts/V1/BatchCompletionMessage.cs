namespace KeeperData.Core.Messaging.Contracts.V1;

public class BatchCompletionMessage : MessageType
{
    public new Guid Id { get; set; } = Guid.NewGuid();
    public BatchType BatchType { get; set; }
    public string? ScanCorrelationId { get; set; }
    public int TotalRecordsProcessed { get; set; }
    public int MessagesPublished { get; set; }
    public DateTime BatchStartTime { get; set; }
    public DateTime BatchCompletionTime { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? Metadata { get; set; }
}

public enum BatchType
{
    SamBulkScan,
    CtsBulkScan
}